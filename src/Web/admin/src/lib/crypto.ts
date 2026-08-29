export class CryptoService {
  private static aesAlgorithm = 'AES-GCM';
  private static rsaAlgorithm = 'RSA-OAEP';

  // Base64 Helpers
  private static arrayBufferToBase64(buffer: ArrayBuffer): string {
    let binary = '';
    const bytes = new Uint8Array(buffer);
    const len = bytes.byteLength;
    for (let i = 0; i < len; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
  }

  private static base64ToArrayBuffer(base64: string): ArrayBuffer {
    const binary = window.atob(base64);
    const len = binary.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
  }

  private static pemToArrayBuffer(pem: string): ArrayBuffer {
    // 兼容 -----BEGIN PUBLIC KEY----- 或 -----BEGIN RSA PUBLIC KEY----- 等格式
    const b64 = pem
      .replace(/-----BEGIN (.*?)-----/g, '')
      .replace(/-----END (.*?)-----/g, '')
      .replace(/\\n/g, '') // 移除转义换行
      .replace(/\s+/g, ''); // 移除所有真正的换行和空格

    return this.base64ToArrayBuffer(b64);
  }

  // 1. 生成 AES-256 密钥
  static async generateAesKey(): Promise<CryptoKey> {
    return await window.crypto.subtle.generateKey(
      { name: this.aesAlgorithm, length: 256 },
      true,
      ['encrypt', 'decrypt']
    );
  }

  // 2. 导出 AES 密钥为 Raw 格式以便通过 RSA 传输
  static async exportAesKey(key: CryptoKey): Promise<ArrayBuffer> {
    return await window.crypto.subtle.exportKey('raw', key);
  }

  // 3. 使用 RSA 公钥加密 AES 密钥
  static async encryptAesKeyWithRsa(rawAesKey: ArrayBuffer, rsaPublicKeyPem: string): Promise<string> {
    const pubKeyBuffer = this.pemToArrayBuffer(rsaPublicKeyPem);
    const importedRsaKey = await window.crypto.subtle.importKey(
      'spki',
      pubKeyBuffer,
      { name: this.rsaAlgorithm, hash: 'SHA-256' },
      false,
      ['encrypt']
    );

    const encryptedAesKey = await window.crypto.subtle.encrypt(
      { name: this.rsaAlgorithm },
      importedRsaKey,
      rawAesKey
    );

    return this.arrayBufferToBase64(encryptedAesKey);
  }

  // 4. 使用 AES-GCM 加密业务数据
  static async encryptDataWithAes(data: any, aesKey: CryptoKey): Promise<string> {
    const jsonString = JSON.stringify(data);
    const encodedData = new TextEncoder().encode(jsonString);

    const nonce = window.crypto.getRandomValues(new Uint8Array(12));

    const cipherText = await window.crypto.subtle.encrypt(
      { name: this.aesAlgorithm, iv: nonce },
      aesKey,
      encodedData
    );

    // .NET 后端预期的格式: [Nonce (12字节)] + [CipherText] + [Tag (16字节)]
    // 恰好 Web Crypto API 的返回格式就是 CipherText + Tag。所以只需要在前面拼上 Nonce 即可。
    const resultBuffer = new Uint8Array(12 + cipherText.byteLength);
    resultBuffer.set(nonce, 0);
    resultBuffer.set(new Uint8Array(cipherText), 12);

    return this.arrayBufferToBase64(resultBuffer.buffer);
  }

  // 5. 使用 AES-GCM 解密业务数据
  static async decryptDataWithAes(base64CipherData: string, aesKey: CryptoKey): Promise<any> {
    const cipherDataBuffer = this.base64ToArrayBuffer(base64CipherData);
    const cipherDataArray = new Uint8Array(cipherDataBuffer);

    // 提取 Nonce (前12字节)
    const nonce = cipherDataArray.slice(0, 12);
    // 提取密文 + 认证标签 Tag (剩下的所有)
    const dataAndTag = cipherDataArray.slice(12);

    const decryptedBuffer = await window.crypto.subtle.decrypt(
      { name: this.aesAlgorithm, iv: nonce },
      aesKey,
      dataAndTag
    );

    const jsonString = new TextDecoder().decode(decryptedBuffer);
    return JSON.parse(jsonString);
  }
}
