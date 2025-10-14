import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export const Navbar = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="bg-blue-600 text-white shadow-lg">
      <div className="container mx-auto px-4">
        <div className="flex justify-between items-center h-16">
          <Link to="/" className="text-2xl font-bold">
            StudentLink
          </Link>

          {user && (
            <div className="flex items-center space-x-4">
              <Link to="/dashboard" className="hover:text-blue-200">
                Dashboard
              </Link>

              {/* Global jobs browse */}
              <Link to="/jobs" className="hover:text-blue-200">
                Jobs
              </Link>

              {/* Student links */}
              {user.role === 'Student' && (
                <>
                  <Link to="/cv-upload" className="hover:text-blue-200">
                    My CV
                  </Link>
                  <Link to="/applications" className="hover:text-blue-200">
                    My Applications
                  </Link>
                </>
              )}

              {/* Recruiter links */}
              {(user.role === 'Recruiter' || user.role === 'Admin') && (
                <>
                  <Link to="/recruiter/jobs" className="hover:text-blue-200">
                    Recruiter Jobs
                  </Link>
                  <Link to="/directory/students" className="hover:text-blue-200">
                    Student Directory
                  </Link>
                </>
              )}

              {/* Admin links */}
              {user.role === 'Admin' && (
                <>
                  <Link to="/admin/users" className="hover:text-blue-200">
                    Admin
                  </Link>
                  <Link to="/admin/directory" className="hover:text-blue-200">
                    Admin Directory
                  </Link>
                </>
              )}

              <Link to="/profile" className="hover:text-blue-200">
                Profile
              </Link>
              <span className="px-3 py-1 bg-blue-700 rounded-full text-sm">
                {user.role}
              </span>
              <span className="text-sm hidden md:inline">{user.email}</span>
              <button
                onClick={handleLogout}
                className="px-4 py-2 bg-red-500 hover:bg-red-600 rounded transition"
              >
                Logout
              </button>
            </div>
          )}
        </div>
      </div>
    </nav>
  );
};