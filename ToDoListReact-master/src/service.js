import axios from 'axios';

const apiUrl = "http://localhost:8080";

// Create axios instance with default config
const api = axios.create({
  baseURL: apiUrl,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Add interceptor to attach token to requests
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Add interceptor to handle response errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error('API Error:', error.response?.data || error.message);
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default {
  getTasks: async () => {
    const result = await api.get('/api/items');
    return result.data;
  },

  addTask: async (name) => {
    const result = await api.post('/api/items', { name });
    return result.data;
  },

  setCompleted: async (id, name, isComplete) => {
    const result = await api.put(`/api/items/${id}`, { name, isComplete });
    return result.data;
  },

  deleteTask: async (id) => {
    await api.delete(`/api/items/${id}`);
  },

  register: async (username, password) => {
    const result = await api.post('/api/auth/register', { username, password });
    if (result.data.token) {
      localStorage.setItem('token', result.data.token);
    }
    return result.data;
  },

  login: async (username, password) => {
    const result = await api.post('/api/auth/login', { username, password });
    if (result.data.token) {
      localStorage.setItem('token', result.data.token);
    }
    return result.data;
  },

  logout: () => {
    localStorage.removeItem('token');
  },

  getCurrentUser: async () => {
    const result = await api.get('/api/auth/me');
    return result.data;
  }
};
