import axios, { AxiosError, AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';

// Create a custom axios instance
export const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '', // Can be configured via .env
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    // You can add token or any custom logic before sending the request
    // const token = localStorage.getItem('token');
    // if (token && config.headers) {
    //   config.headers.Authorization = `Bearer ${token}`;
    // }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    // Modify response data if needed
    return response;
  },
  (error: AxiosError) => {
    // Global error handling
    if (error.response?.status === 401) {
      // Handle unauthorized errors (e.g. redirect to login or refresh token)
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
