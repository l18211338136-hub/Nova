import axios, { AxiosError, AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { useAuthStore } from '@/stores/auth-store';
import { queryClient } from '@/main';
import { CryptoService } from './crypto';

declare module 'axios' {
  export interface AxiosRequestConfig {
    aesKey?: CryptoKey;
    skipEncryption?: boolean;
  }
}

let rsaPublicKey: string | null = null;
let pendingPublicKeyPromise: Promise<string | null> | null = null;

async function getRsaPublicKey() {
  if (rsaPublicKey) return rsaPublicKey;
  if (import.meta.env.VITE_RSA_PUBLIC_KEY) {
    rsaPublicKey = import.meta.env.VITE_RSA_PUBLIC_KEY.replace(/\\n/g, '\n');
    return rsaPublicKey;
  }
  
  if (pendingPublicKeyPromise) {
    return pendingPublicKeyPromise;
  }

  pendingPublicKeyPromise = (async () => {
    try {
      // 按需动态导入，打破 api-client.ts 与自动生成代码之间的循环依赖
      const { publicKey } = await import('@/api/endpoints/security');
      
      // 调用 Orval 生成的强类型方法
      const res = await publicKey({ skipEncryption: true });
      
      // res 经过 customInstance 解包后已经是真正的 ApiResponse，所以读取 data.publicKey
      rsaPublicKey = res.data?.publicKey || null;
      return rsaPublicKey;
    } catch (e) {
      console.error('Failed to fetch RSA public key', e);
      return null;
    } finally {
      pendingPublicKeyPromise = null;
    }
  })();

  return pendingPublicKeyPromise;
}

// Create a custom axios instance
export const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '', // Can be configured via .env
  timeout: 120000,
  headers: {
    'Content-Type': 'application/json',
  },
});

let isRefreshing = false;
let failedQueue: any[] = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Request interceptor
apiClient.interceptors.request.use(
  async (config) => {
    const token = useAuthStore.getState().auth.accessToken;
    if (token && config.headers) {
      config.headers['Authorization'] = `Bearer ${token}`;
    }

    // 凡是非文件上传的请求，统统开启安全传输通道（就算没有 Body 的 GET，也要生成钥匙给后端用于加密返回值）
    if (!(config.data instanceof FormData) && !config.skipEncryption) {
      const pubKey = await getRsaPublicKey();
      if (pubKey) {
        // 1. 生成本次请求专用的随机 AES 密钥
        const aesKey = await CryptoService.generateAesKey();
        
        // 2. 将 AES 密钥保存到 config 中，传递给响应拦截器
        config.aesKey = aesKey;

        // 3. 导出并使用 RSA 加密 AES 密钥，塞进 Header
        const rawAesKey = await CryptoService.exportAesKey(aesKey);
        const encryptedAesKeyBase64 = await CryptoService.encryptAesKeyWithRsa(rawAesKey, pubKey);
        
        if (config.headers && typeof config.headers.set === 'function') {
          config.headers.set('X-Encryption-Key', encryptedAesKeyBase64);
          config.headers.set('Content-Type', 'application/json');
        } else if (config.headers) {
          config.headers['X-Encryption-Key'] = encryptedAesKeyBase64;
          config.headers['Content-Type'] = 'application/json';
        }

        // 4. 使用 AES 加密真实的业务 Payload (只有确实带有 Payload 的时候才去加密它)
        if (config.data) {
          const encryptedBodyBase64 = await CryptoService.encryptDataWithAes(config.data, aesKey);
          config.data = encryptedBodyBase64;
        }
      }
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor
apiClient.interceptors.response.use(
  async (response: AxiosResponse) => {
    // 检查 config 中是否带有本次请求的 AES 密钥，且返回的是否是纯文本密文
    const aesKey = (response.config as any).aesKey as CryptoKey;
    if (aesKey && typeof response.data === 'string' && response.data.length > 0) {
      try {
        const decryptedData = await CryptoService.decryptDataWithAes(response.data, aesKey);
        response.data = decryptedData;
      } catch (e) {
        console.error('Failed to decrypt response payload', e);
        // 如果解密失败，直接抛出异常避免走后续业务逻辑
        return Promise.reject(new Error('Failed to decrypt response'));
      }
    }
    return response;
  },
  async (error: AxiosError) => {
    const originalRequest = error.config as any;

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise(function (resolve, reject) {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers['Authorization'] = 'Bearer ' + token;
            return apiClient(originalRequest);
          })
          .catch((err) => {
            return Promise.reject(err);
          });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = useAuthStore.getState().auth.refreshToken;
      const accessToken = useAuthStore.getState().auth.accessToken;
      if (!refreshToken || !accessToken) {
        useAuthStore.getState().auth.reset();
        queryClient.clear();
        window.location.href = '/sign-in';
        return Promise.reject(error);
      }

      try {
        const accessToken = useAuthStore.getState().auth.accessToken;
        const { data } = await axios.post(
          `${import.meta.env.VITE_API_URL || ''}/api/identity/refresh`,
          {
            accessToken: accessToken,
            refreshToken: refreshToken
          }
        );

        if (data && data.data && data.data.token) {
          useAuthStore.getState().auth.setAccessToken(data.data.token);
          if (data.data.refreshToken) {
            useAuthStore.getState().auth.setRefreshToken(data.data.refreshToken);
          }

          processQueue(null, data.data.token);

          originalRequest.headers['Authorization'] = 'Bearer ' + data.data.token;
          return apiClient(originalRequest);
        } else {
          throw new Error('Invalid refresh response');
        }
      } catch (err) {
        processQueue(err, null);
        useAuthStore.getState().auth.reset();
        queryClient.clear();
        window.location.href = '/sign-in';
        return Promise.reject(err);
      } finally {
        isRefreshing = false;
      }
    } else if (error.response) {
      // 保持让 React Query / handleServerError 拦截并抛出 Toast 提示，不强制全页跳转重定向
    }

    return Promise.reject(error);
  }
);

// Generic Orval mutator wrapper
export const customInstance = <T>(
  config: AxiosRequestConfig,
  options?: AxiosRequestConfig
): Promise<T> => {
  const source = axios.CancelToken.source();
  const promise = apiClient({
    ...config,
    ...options,
    cancelToken: source.token,
  }).then(({ data }) => data);

  // @ts-expect-error adding cancel function to promise
  promise.cancel = () => {
    source.cancel('Query was cancelled');
  };

  return promise;
};
