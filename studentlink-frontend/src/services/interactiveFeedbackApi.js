import api from './api';

// Get interactive feedback for a CV
export const getInteractiveFeedback = async (cvId) => {
  const response = await api.get(`/cv/interactive/${cvId}/feedback`);
  return response.data;
};

// Get improvement progress
export const getImprovementProgress = async () => {
  const response = await api.get('/cv/interactive/progress');
  return response.data;
};

// Mark action as completed
export const markActionCompleted = async (cvId, actionIndex) => {
  const response = await api.post(`/cv/interactive/${cvId}/action/${actionIndex}/complete`, {});
  return response.data;
};

// Get interactive dashboard data
export const getInteractiveDashboard = async () => {
  const response = await api.get('/cv/interactive/dashboard');
  return response.data;
};