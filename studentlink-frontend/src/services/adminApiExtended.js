import axios from 'axios';

const API_URL = process.env.REACT_APP_API_URL || 'https://localhost:7068';
const authHeaders = () => ({ Authorization: `Bearer ${localStorage.getItem('token')}` });

export const superAdminApi = {
  // Students
  students: (q) => axios.get(`${API_URL}/admin/students`, { params: { q }, headers: authHeaders() }),
  studentCvs: (id) => axios.get(`${API_URL}/admin/students/${id}/cvs`, { headers: authHeaders() }),
  downloadCv: (cvId) => axios.get(`${API_URL}/admin/cv/${cvId}/download`, { headers: authHeaders(), responseType: 'blob' }),
  // Recruiters
  recruiters: (q) => axios.get(`${API_URL}/admin/recruiters`, { params: { q }, headers: authHeaders() }),
  allJobs: (q) => axios.get(`${API_URL}/admin/jobs`, { params: { q }, headers: authHeaders() }),
  recruiterJobs: (id, q) => axios.get(`${API_URL}/admin/recruiters/${id}/jobs`, { params: { q }, headers: authHeaders() })
};
