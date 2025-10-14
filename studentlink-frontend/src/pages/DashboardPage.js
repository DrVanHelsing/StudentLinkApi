import React from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useNavigate } from 'react-router-dom';
import ICONS from '../utils/icons';

export const DashboardPage = () => {
  const { user } = useAuth();

  const renderDashboard = () => {
    switch (user.role) {
      case 'Student':
        return <StudentDashboard user={user} />;
      case 'Recruiter':
        return <RecruiterDashboard user={user} />;
      case 'Admin':
        return <AdminDashboard user={user} />;
      default:
        return <div>Unknown role</div>;
    }
  };

  return <>{renderDashboard()}</>;
};

const StudentDashboard = ({ user }) => {
  const navigate = useNavigate();
  
  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-800 mb-6">
        Welcome back, {user.firstName || 'Student'}!
      </h1>
      
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <StatCard title="Profile Completeness" value="45%" color="blue" />
        <StatCard title="Applications" value="3" color="green" />
        <StatCard title="Messages" value="12" color="purple" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4">Quick Actions</h2>
          <div className="space-y-3">
            <ActionButton 
              text="Upload CV" 
              icon={ICONS.upload} 
              onClick={() => navigate('/cv-upload')} 
            />
            <ActionButton 
              text="Browse Jobs" 
              icon={ICONS.search} 
              onClick={() => navigate('/jobs')} 
            />
            <ActionButton 
              text="Edit Profile" 
              icon={ICONS.edit}
              onClick={() => navigate('/profile')}
            />
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-xl font-semibold mb-4">Recent Activity</h2>
          <ul className="space-y-3 text-gray-700">
            <li>{ICONS.document} Your CV was reviewed 2 days ago</li>
            <li>{ICONS.job} You applied to 1 new job</li>
            <li>{ICONS.notification} New feedback is available</li>
          </ul>
        </div>
      </div>
    </div>
  );
};

const RecruiterDashboard = ({ user }) => (
  <div>
    <h1 className="text-3xl font-bold text-gray-800 mb-6">
      Welcome back, {user.firstName || 'Recruiter'}!
    </h1>
    <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
      <StatCard title="Active Jobs" value="4" color="blue" />
      <StatCard title="Applicants" value="12" color="green" />
      <StatCard title="Interviews" value="3" color="purple" />
    </div>
    <div className="bg-white rounded-lg shadow p-6">
      <h2 className="text-xl font-semibold mb-4">Tips</h2>
      <p className="text-gray-700">Keep your job posts updated to attract more applicants.</p>
    </div>
  </div>
);

const AdminDashboard = ({ user }) => (
  <div>
    <h1 className="text-3xl font-bold text-gray-800 mb-6">
      Welcome back, {user.firstName || 'Admin'}!
    </h1>
    <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
      <StatCard title="Users" value="128" color="blue" />
      <StatCard title="Jobs" value="35" color="green" />
      <StatCard title="Applications" value="220" color="purple" />
      <StatCard title="CVs" value="156" color="orange" />
    </div>
    <div className="bg-white rounded-lg shadow p-6">
      <h2 className="text-xl font-semibold mb-4">System Notices</h2>
      <p className="text-gray-700">All systems operational.</p>
    </div>
  </div>
);

const StatCard = ({ title, value, color }) => (
  <div className={`bg-${color}-50 rounded-lg shadow p-6`}>
    <div className="text-sm text-gray-600">{title}</div>
    <div className="text-3xl font-bold mt-2">{value}</div>
  </div>
);

const ActionButton = ({ text, icon, onClick }) => (
  <button onClick={onClick} className="w-full px-4 py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition">
    <span className="mr-2">{icon}</span>
    {text}
  </button>
);