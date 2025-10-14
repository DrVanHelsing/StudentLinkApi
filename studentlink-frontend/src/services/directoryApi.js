import axios from 'axios';

const API_URL = process.env.REACT_APP_API_URL || 'https://localhost:7068';
const authHeaders = () => ({ Authorization: `Bearer ${localStorage.getItem('token')}` });

export const directoryApi = {
  students: (q, skill) => axios.get(`${API_URL}/directory/students`, { params: { q, skill }, headers: authHeaders() }),
  studentCvs: (id) => axios.get(`${API_URL}/directory/students/${id}/cvs`, { headers: authHeaders() })
};
