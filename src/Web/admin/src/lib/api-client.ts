import axios, { AxiosError, AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { useAuthStore } from '@/stores/auth-store';

// Create a custom axios instance
export const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '', // Can be configured via .env
  timeout: 10000,
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
  (config) => {
    const token = useAuthStore.getState().auth.accessToken;
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    // 优先从 localStorage 获取 tenant，如果没有则默认使用 'root'
    if (config.headers && !config.headers['X-Tenant-Id']) {
      const storedTenant = localStorage.getItem('tenant');
      config.headers['X-Tenant-Id'] = storedTenant || 'root';
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
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
      if (!refreshToken) {
        useAuthStore.getState().auth.reset();
        window.location.href = '/sign-in';
        return Promise.reject(error);
      }

      try {
        const { data } = await axios.post(`${import.meta.env.VITE_API_URL || ''}/api/identity/refresh`, {
          refreshToken: refreshToken
        });

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
        window.location.href = '/sign-in';
        return Promise.reject(err);
      } finally {
        isRefreshing = false;
      }
    } else if (error.response) {
      const status = error.response.status;
      if (status === 403) {
        window.location.href = '/403';
      } else if (status === 404) {
        window.location.href = '/404';
      } else if (status === 500) {
        window.location.href = '/500';
      } else if (status === 503) {
        window.location.href = '/503';
      }
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
