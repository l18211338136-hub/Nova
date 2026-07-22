import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import enUS from '../locales/en-US/translation.json';
import zhCN from '../locales/zh-CN/translation.json';

const defaultLanguage = localStorage.getItem('i18nextLng') || 'zh';

i18n
  .use(initReactI18next)
  .init({
    resources: {
      en: {
        translation: enUS,
      },
      zh: {
        translation: zhCN,
      },
    },
    lng: defaultLanguage, // read from localStorage
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false, // react already safes from xss
    },
  });

export default i18n;
