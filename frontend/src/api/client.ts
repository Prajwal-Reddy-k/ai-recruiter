import axios from "axios";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

function clearSessionAndRedirect() {
  localStorage.removeItem("token");
  localStorage.removeItem("refreshToken");
  localStorage.removeItem("user");
  if (!window.location.pathname.startsWith("/login")) {
    window.location.href = "/login";
  }
}

// Shared in-flight refresh call — concurrent requests that all 401 at once await the SAME
// promise instead of each triggering their own /auth/refresh call.
let refreshPromise: Promise<string> | null = null;

function refreshAccessToken(): Promise<string> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    const storedRefreshToken = localStorage.getItem("refreshToken");
    if (!storedRefreshToken) {
      throw new Error("No refresh token available.");
    }
    // Plain axios (not apiClient) — avoids re-entering these same interceptors and the
    // request-interceptor attaching a stale/expired access token to the refresh call.
    const { data } = await axios.post(`${import.meta.env.VITE_API_BASE_URL}/auth/refresh`, {
      refreshToken: storedRefreshToken,
    });
    localStorage.setItem("token", data.token);
    localStorage.setItem("refreshToken", data.refreshToken);
    return data.token as string;
  })().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
}

// A 401 on a request that carried our own bearer token means the token expired or its
// embedded security stamp no longer matches the server (e.g. after a password reset). Rather
// than immediately signing the user out, silently attempt a refresh and retry the original
// request once — only falling back to a hard logout/redirect if the refresh itself fails.
// Deliberately checked against the outgoing request's own Authorization header (not just "was
// this a 401"), so a wrong password on the login form — also a 401, but with no token attached
// — never triggers this and wipes the form before the user sees the error.
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const hadAuthHeader = Boolean(originalRequest?.headers?.Authorization);

    if (error.response?.status === 401 && hadAuthHeader && !originalRequest._retry) {
      originalRequest._retry = true;
      try {
        const newAccessToken = await refreshAccessToken();
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        return apiClient(originalRequest);
      } catch {
        clearSessionAndRedirect();
        return Promise.reject(error);
      }
    }

    if (error.response?.status === 401 && hadAuthHeader && originalRequest?._retry) {
      // The refreshed access token was itself rejected — the session is unrecoverable.
      clearSessionAndRedirect();
    }

    return Promise.reject(error);
  },
);

export default apiClient;
