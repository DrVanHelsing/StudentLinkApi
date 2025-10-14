import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { DashboardPage } from './pages/DashboardPage';
import { ProfilePage } from './pages/ProfilePage';
import { CVUploadPage } from './pages/CVUploadPage';
import InteractiveFeedbackPage from './pages/InteractiveFeedbackPage';
import ProgressDashboardPage from './pages/ProgressDashboardPage';
import { JobsBrowsePage, RecruiterJobsPage, JobApplicationsPage, AdminUsersPage } from './pages/RolePages';
import { AdminDirectoryPage } from './pages/AdminDirectoryPage';
import { StudentDirectoryPage } from './pages/StudentDirectoryPage';
import './index.css';

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          
          {/* Authenticated routes with navbar */}
          <Route 
            path="/dashboard" 
            element={
              <ProtectedRoute>
                <WithNav>
                  <DashboardPage />
                </WithNav>
              </ProtectedRoute>
            } 
          />
          
          <Route 
            path="/profile" 
            element={
              <ProtectedRoute>
                <WithNav>
                  <ProfilePage />
                </WithNav>
              </ProtectedRoute>
            } 
          />
          
          <Route 
            path="/cv-upload" 
            element={
              <ProtectedRoute allowedRoles={['Student']}>
                <WithNav>
                  <CVUploadPage />
                </WithNav>
              </ProtectedRoute>
            } 
          />
          
          <Route 
            path="/cv-feedback/:cvId" 
            element={
              <ProtectedRoute allowedRoles={['Student']}>
                <WithNav>
                  <InteractiveFeedbackPage />
                </WithNav>
              </ProtectedRoute>
            } 
          />
          
          <Route 
            path="/cv-progress" 
            element={
              <ProtectedRoute allowedRoles={['Student']}>
                <WithNav>
                  <ProgressDashboardPage />
                </WithNav>
              </ProtectedRoute>
            } 
          />

          {/* Student routes */}
          <Route 
            path="/jobs" 
            element={
              <ProtectedRoute>
                <WithNav>
                  <JobsBrowsePage />
                </WithNav>
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/applications" 
            element={
              <ProtectedRoute allowedRoles={['Student']}>
                <WithNav>
                  <JobApplicationsPage />
                </WithNav>
              </ProtectedRoute>
            } 
          />

          {/* Recruiter routes */}
          <Route 
            path="/recruiter/jobs" 
            element={
              <ProtectedRoute allowedRoles={['Recruiter','Admin']}>
                <WithNav>
                  <RecruiterJobsPage />
                </WithNav>
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/directory/students" 
            element={
              <ProtectedRoute allowedRoles={['Recruiter','Admin']}>
                <WithNav>
                  <StudentDirectoryPage />
                </WithNav>
              </ProtectedRoute>
            } 
          />

          {/* Admin routes */}
          <Route 
            path="/admin/users" 
            element={
              <ProtectedRoute allowedRoles={['Admin']}>
                <WithNav>
                  <AdminUsersPage />
                </WithNav>
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/admin/directory" 
            element={
              <ProtectedRoute allowedRoles={['Admin']}>
                <WithNav>
                  <AdminDirectoryPage />
                </WithNav>
              </ProtectedRoute>
            } 
          />
          
          <Route path="/" element={<HomePage />} />
          <Route path="/unauthorized" element={<UnauthorizedPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

const WithNav = ({ children }) => {
  const { Navbar } = require('./components/Navbar');
  return (
    <div className="min-h-screen bg-gray-100">
      <Navbar />
      <div className="container mx-auto px-4 py-8">
        {children}
      </div>
    </div>
  );
};

const HomePage = () => {
  const { user } = useAuth();
  
  if (user) {
    return <Navigate to="/dashboard" replace />;
  }
  
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center px-4">
      <div className="text-center text-white">
        <h1 className="text-6xl font-bold mb-4">StudentLink</h1>
        <p className="text-2xl mb-8">Connect Students with Opportunities</p>
        <div className="space-x-4">
          <a 
            href="/login"
            className="inline-block px-8 py-3 bg-white text-blue-600 font-semibold rounded-lg hover:bg-gray-100 transition"
          >
            Sign In
          </a>
          <a 
            href="/register"
            className="inline-block px-8 py-3 bg-blue-700 text-white font-semibold rounded-lg hover:bg-blue-800 transition"
          >
            Get Started
          </a>
        </div>
      </div>
    </div>
  );
};

const UnauthorizedPage = () => (
  <div className="min-h-screen bg-gray-100 flex items-center justify-center px-4">
    <div className="text-center">
      <h1 className="text-6xl font-bold text-gray-800 mb-4">403</h1>
      <p className="text-2xl text-gray-600 mb-8">Unauthorized Access</p>
      <a 
        href="/dashboard"
        className="inline-block px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
      >
        Go to Dashboard
      </a>
    </div>
  </div>
);

const NotFoundPage = () => (
  <div className="min-h-screen bg-gray-100 flex items-center justify-center px-4">
    <div className="text-center">
      <h1 className="text-6xl font-bold text-gray-800 mb-4">404</h1>
      <p className="text-2xl text-gray-600 mb-8">Page Not Found</p>
      <a 
        href="/"
        className="inline-block px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
      >
        Go Home
      </a>
    </div>
  </div>
);

export default App;