import axios from 'axios';
import api from './api';

const API_URL = process.env.REACT_APP_API_URL || 'https://localhost:7068';

const authHeaders = () => ({
  Authorization: `Bearer ${localStorage.getItem('token')}`,
  'Content-Type': 'application/json'
});

export const jobsApi = {
  search: (params) => axios.get(`${API_URL}/jobs`, { params }),
  get: (id) => axios.get(`${API_URL}/jobs/${id}`),
  mine: () => axios.get(`${API_URL}/jobs/mine`, { headers: authHeaders() }),
  create: (data) => axios.post(`${API_URL}/jobs`, data, { headers: authHeaders() }),
  update: (id, data) => axios.put(`${API_URL}/jobs/${id}`, data, { headers: authHeaders() }),
  remove: (id) => axios.delete(`${API_URL}/jobs/${id}`, { headers: authHeaders() }),
  apply: (id, notes) => axios.post(`${API_URL}/jobs/${id}/apply`, JSON.stringify(notes ?? ''), { headers: authHeaders() }),
  myApplications: () => axios.get(`${API_URL}/jobs/applications/me`, { headers: authHeaders() }),
  getApplicationsForJob: (id) => axios.get(`${API_URL}/jobs/${id}/applications`, { headers: authHeaders() }),
  updateApplicationStatus: (appId, status) => axios.put(`${API_URL}/jobs/applications/${appId}/status`, JSON.stringify(status), { headers: authHeaders() })
};

export const adminApi = {
  users: () => axios.get(`${API_URL}/admin/users`, { headers: authHeaders() }),
  setUserStatus: (id, active) => axios.put(`${API_URL}/admin/users/${id}/status`, JSON.stringify(active), { headers: authHeaders() }),
  setUserRole: (id, role) => axios.put(`${API_URL}/admin/users/${id}/role`, JSON.stringify(role), { headers: authHeaders() }),
  stats: () => axios.get(`${API_URL}/admin/stats`, { headers: authHeaders() })
};

// CV endpoints consolidated here
export const cvApi = {
  current: () => api.get('/cv/current'),
  upload: (file) => {
    const form = new FormData();
    form.append('file', file);
    return api.post('/cv/upload', form, { headers: { 'Content-Type': 'multipart/form-data' } });
  },
  history: () => api.get('/cv/history'),
  feedback: (cvId) => api.get(`/cv/${cvId}/feedback`),
  analysis: (cvId) => api.get(`/cv/${cvId}/analysis`),
  reprocess: (cvId) => api.post(`/cv/${cvId}/reprocess`),
  download: (cvId) => api.get(`/cv/download/${cvId}`, { responseType: 'blob' }),
  delete: (cvId) => api.delete(`/cv/${cvId}`)
};
