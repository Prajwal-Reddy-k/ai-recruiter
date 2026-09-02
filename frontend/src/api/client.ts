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

// A 401 on a request that carried our own bearer token means the token expired or its
// embedded security stamp no longer matches the server (e.g. after a password reset) —
// the session is dead either way. Without this, pages just show a confusing "failed to
// load" instead of sending the user back to sign in. Deliberately checked against the
// outgoing request's own Authorization header (not just "was this a 401"), so a wrong
// password on the login form — also a 401, but with no token attached — never triggers
// this and wipes the form before the user sees the error.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const hadAuthHeader = Boolean(error.config?.headers?.Authorization);
    if (error.response?.status === 401 && hadAuthHeader) {
      localStorage.removeItem("token");
      localStorage.removeItem("user");
      if (!window.location.pathname.startsWith("/login")) {
        window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  },
);

export default apiClient;
