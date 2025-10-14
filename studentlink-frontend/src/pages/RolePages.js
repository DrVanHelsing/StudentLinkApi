import React, { useEffect, useState, useCallback } from 'react';
import { jobsApi, adminApi } from '../services/adminRecruiterStudentApi';
import { useAuth } from '../contexts/AuthContext';
import ICONS from '../utils/icons';

const JobCard = ({ job, onApply, canManage, onEdit, onDelete, onViewApps }) => (
  <div className="bg-white rounded-lg shadow-md hover:shadow-lg transition-shadow p-6 border border-gray-200">
    <div className="flex justify-between items-start mb-4">
      <div className="flex-1">
        <h3 className="text-xl font-bold text-gray-900 mb-2">{job.title}</h3>
        <p className="text-gray-600 line-clamp-2 mb-3">{job.description}</p>
        <div className="flex flex-wrap gap-3 text-sm text-gray-700">
          {job.location && (
            <span className="flex items-center gap-1">
              {ICONS.location} {job.location}
            </span>
          )}
          {job.jobType && (
            <span className="flex items-center gap-1">
              {ICONS.calendar} {job.jobType}
            </span>
          )}
          {job.salaryMin && job.salaryMax && (
            <span className="flex items-center gap-1">
              {ICONS.money} ${job.salaryMin.toLocaleString()} - ${job.salaryMax.toLocaleString()}
            </span>
          )}
        </div>
        {job.requiredSkills && (
          <div className="mt-3 flex flex-wrap gap-2">
            {job.requiredSkills.split(',').map((skill, i) => (
              <span key={i} className="px-2 py-1 bg-blue-50 text-blue-700 rounded-md text-xs font-medium">
                {skill.trim()}
              </span>
            ))}
          </div>
        )}
      </div>
      <span className={`px-3 py-1 rounded-full text-xs font-semibold ml-4 ${job.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-200 text-gray-700'}`}>
        {job.isActive ? `${ICONS.active} Active` : 'Closed'}
      </span>
    </div>

    <div className="mt-4 pt-4 border-t border-gray-200 flex gap-2 justify-end flex-wrap">
      {onApply && (
        <button className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium shadow-sm" onClick={onApply}>
          <span className="flex items-center gap-1">{ICONS.apply} Apply Now</span>
        </button>
      )}
      {canManage && (
        <>
          <button className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors font-medium" onClick={onEdit}>
            <span className="flex items-center gap-1">{ICONS.edit} Edit</span>
          </button>
          <button className="px-4 py-2 bg-purple-100 text-purple-700 rounded-lg hover:bg-purple-200 transition-colors font-medium" onClick={onViewApps}>
            <span className="flex items-center gap-1">{ICONS.chart} Applicants</span>
          </button>
          <button className="px-4 py-2 bg-red-100 text-red-700 rounded-lg hover:bg-red-200 transition-colors font-medium" onClick={onDelete}>
            <span className="flex items-center gap-1">{ICONS.delete} Delete</span>
          </button>
        </>
      )}
    </div>
  </div>
);

export const JobsBrowsePage = () => {
  const [jobs, setJobs] = useState([]);
  const [q, setQ] = useState('');
  const [location, setLocation] = useState('');
  const [jobType, setJobType] = useState('');
  const { user } = useAuth();

  const load = useCallback(async () => {
    const res = await jobsApi.search({ q, location, jobType });
    setJobs(res.data);
  }, [q, location, jobType]);

  useEffect(() => { load(); }, [load]);

  const apply = async (id) => {
    try {
      await jobsApi.apply(id, '');
      alert(`${ICONS.check} Application submitted successfully!`);
    } catch (err) {
      if (err.response?.status === 409) {
        alert(`${ICONS.warning} You have already applied to this job`);
      } else {
        alert(`${ICONS.rejected} Failed to apply. Please try again.`);
      }
    }
  };

  const canApply = user?.role === 'Student';

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 py-8">
      <div className="max-w-6xl mx-auto px-4">
        <div className="mb-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">Find Your Dream Job</h1>
          <p className="text-gray-600">Browse through {jobs.length} available positions</p>
        </div>
        
        <div className="bg-white rounded-xl shadow-md p-6 mb-6">
          <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
            <input 
              className="col-span-2 border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-blue-500 focus:border-transparent" 
              placeholder={`${ICONS.search} Search jobs...`} 
              value={q} 
              onChange={e=>setQ(e.target.value)} 
            />
            <input 
              className="border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-blue-500 focus:border-transparent" 
              placeholder={`${ICONS.location} Location`} 
              value={location} 
              onChange={e=>setLocation(e.target.value)} 
            />
            <select 
              className="border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-blue-500 focus:border-transparent" 
              value={jobType} 
              onChange={e=>setJobType(e.target.value)}
            >
              <option value="">All Types</option>
              <option>Full-time</option>
              <option>Part-time</option>
              <option>Contract</option>
              <option>Internship</option>
            </select>
            <button 
              className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium shadow-md flex items-center justify-center gap-2" 
              onClick={load}
            >
              <span>{ICONS.search}</span> Search
            </button>
          </div>
        </div>

        <div className="grid grid-cols-1 gap-4">
          {jobs.map(j => (
            <JobCard key={j.id} job={j} onApply={canApply ? () => apply(j.id) : undefined} />
          ))}
          {jobs.length === 0 && (
            <div className="text-center py-12 bg-white rounded-xl shadow-md">
              <div className="text-6xl mb-4">{ICONS.search}</div>
              <p className="text-xl text-gray-600">No jobs found. Try adjusting your search filters.</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export const RecruiterJobsPage = () => {
  const [jobs, setJobs] = useState([]);
  const [form, setForm] = useState({ title: '', description: '', location: '', jobType: 'Full-time', requiredSkills: '', salaryMin: '', salaryMax: '' });
  const [appsOpenFor, setAppsOpenFor] = useState(null);
  const [apps, setApps] = useState([]);
  const [editing, setEditing] = useState(null);
  const [editData, setEditData] = useState(null);
  const [showForm, setShowForm] = useState(false);

  const load = useCallback(async () => {
    const res = await jobsApi.mine();
    setJobs(res.data);
  }, []);

  useEffect(() => { load(); }, [load]);

  const create = async () => {
    if (!form.title || !form.description) return alert('Title and description required');
    const data = {
      ...form,
      salaryMin: form.salaryMin ? parseFloat(form.salaryMin) : null,
      salaryMax: form.salaryMax ? parseFloat(form.salaryMax) : null
    };
    await jobsApi.create(data);
    setForm({ title: '', description: '', location: '', jobType: 'Full-time', requiredSkills: '', salaryMin: '', salaryMax: '' });
    setShowForm(false);
    await load();
  };

  const del = async (id) => {
    if (!window.confirm('Delete this job posting?')) return;
    await jobsApi.remove(id);
    await load();
  };

  const viewApps = async (id) => {
    setAppsOpenFor(id);
    const res = await jobsApi.getApplicationsForJob(id);
    setApps(res.data);
  };

  const updateAppStatus = async (appId, status) => {
    await jobsApi.updateApplicationStatus(appId, status);
    if (appsOpenFor) {
      const res = await jobsApi.getApplicationsForJob(appsOpenFor);
      setApps(res.data);
    }
  };

  const beginEdit = (job) => {
    setEditing(job.id);
    setEditData({ ...job, salaryMin: job.salaryMin || '', salaryMax: job.salaryMax || '' });
  };

  const saveEdit = async () => {
    const data = {
      ...editData,
      salaryMin: editData.salaryMin ? parseFloat(editData.salaryMin) : null,
      salaryMax: editData.salaryMax ? parseFloat(editData.salaryMax) : null
    };
    await jobsApi.update(editing, data);
    setEditing(null);
    setEditData(null);
    await load();
  };

  const cancelEdit = () => {
    setEditing(null);
    setEditData(null);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-purple-50 to-pink-100 py-8">
      <div className="max-w-7xl mx-auto px-4">
        <div className="flex justify-between items-center mb-8">
          <div>
            <h1 className="text-4xl font-bold text-gray-900 mb-2">My Job Postings</h1>
            <p className="text-gray-600">Manage your job listings</p>
          </div>
          <button 
            className="px-6 py-3 bg-gradient-to-r from-blue-600 to-purple-600 text-white rounded-lg hover:shadow-lg transition-shadow font-medium flex items-center gap-2"
            onClick={() => setShowForm(!showForm)}
          >
            {showForm ? `${ICONS.cancel} Cancel` : `${ICONS.upload} Post New Job`}
          </button>
        </div>

        {showForm && (
          <div className="bg-white rounded-xl shadow-lg p-6 mb-6 border-2 border-blue-200">
            <h2 className="text-2xl font-bold mb-4 text-gray-900">Create New Job</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <input className="border rounded-lg p-3 focus:ring-2 focus:ring-blue-500" placeholder="Job Title *" value={form.title} onChange={e=>setForm({...form,title:e.target.value})} />
              <input className="border rounded-lg p-3 focus:ring-2 focus:ring-blue-500" placeholder="Location" value={form.location} onChange={e=>setForm({...form,location:e.target.value})} />
              <select className="border rounded-lg p-3 focus:ring-2 focus:ring-blue-500" value={form.jobType} onChange={e=>setForm({...form,jobType:e.target.value})}>
                <option>Full-time</option>
                <option>Part-time</option>
                <option>Contract</option>
                <option>Internship</option>
              </select>
              <input className="border rounded-lg p-3 focus:ring-2 focus:ring-blue-500" placeholder="Required Skills (comma separated)" value={form.requiredSkills} onChange={e=>setForm({...form,requiredSkills:e.target.value})} />
              <input type="number" className="border rounded-lg p-3 focus:ring-2 focus:ring-blue-500" placeholder="Min Salary" value={form.salaryMin} onChange={e=>setForm({...form,salaryMin:e.target.value})} />
              <input type="number" className="border rounded-lg p-3 focus:ring-2 focus:ring-blue-500" placeholder="Max Salary" value={form.salaryMax} onChange={e=>setForm({...form,salaryMax:e.target.value})} />
              <textarea className="border rounded-lg p-3 col-span-1 md:col-span-2 focus:ring-2 focus:ring-blue-500" rows={5} placeholder="Job Description *" value={form.description} onChange={e=>setForm({...form,description:e.target.value})} />
              <div className="col-span-1 md:col-span-2 text-right">
                <button className="px-8 py-3 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors font-medium shadow-md flex items-center gap-2 ml-auto" onClick={create}>
                  <span>{ICONS.upload}</span> Post Job
                </button>
              </div>
            </div>
          </div>
        )}

        <div className="space-y-4">
          {jobs.map(j => (
            <div key={j.id}>
              {editing === j.id ? (
                <div className="bg-white rounded-xl shadow-md p-6 border-2 border-purple-300">
                  <h3 className="text-xl font-bold mb-4">Edit Job</h3>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <input className="border rounded-lg p-3" value={editData.title} onChange={e=>setEditData({...editData,title:e.target.value})} />
                    <input className="border rounded-lg p-3" value={editData.location || ''} onChange={e=>setEditData({...editData,location:e.target.value})} />
                    <select className="border rounded-lg p-3" value={editData.jobType || ''} onChange={e=>setEditData({...editData,jobType:e.target.value})}>
                      <option>Full-time</option>
                      <option>Part-time</option>
                      <option>Contract</option>
                      <option>Internship</option>
                    </select>
                    <input className="border rounded-lg p-3" value={editData.requiredSkills || ''} onChange={e=>setEditData({...editData,requiredSkills:e.target.value})} />
                    <input type="number" className="border rounded-lg p-3" placeholder="Min Salary" value={editData.salaryMin} onChange={e=>setEditData({...editData,salaryMin:e.target.value})} />
                    <input type="number" className="border rounded-lg p-3" placeholder="Max Salary" value={editData.salaryMax} onChange={e=>setEditData({...editData,salaryMax:e.target.value})} />
                    <textarea className="border rounded-lg p-3 col-span-1 md:col-span-2" rows={4} value={editData.description} onChange={e=>setEditData({...editData,description:e.target.value})} />
                    <label className="flex items-center gap-2 col-span-1 md:col-span-2">
                      <input type="checkbox" checked={!!editData.isActive} onChange={e=>setEditData({...editData,isActive:e.target.checked})} className="w-4 h-4" />
                      <span className="font-medium">Active</span>
                    </label>
                    <div className="col-span-1 md:col-span-2 flex gap-3 justify-end">
                      <button className="px-6 py-2 bg-gray-200 rounded-lg hover:bg-gray-300" onClick={cancelEdit}>Cancel</button>
                      <button className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700" onClick={saveEdit}>Save Changes</button>
                    </div>
                  </div>
                </div>
              ) : (
                <>
                  <JobCard 
                    job={j}
                    canManage
                    onEdit={() => beginEdit(j)}
                    onDelete={() => del(j.id)}
                    onViewApps={() => viewApps(j.id)}
                  />
                  
                  {appsOpenFor === j.id && (
                    <div className="mt-4 bg-gradient-to-r from-blue-50 to-purple-50 rounded-xl shadow-md p-6 border-l-4 border-blue-500">
                      <h4 className="text-xl font-bold mb-4 text-gray-900 flex items-center gap-2">
                        <span>{ICONS.students}</span> Applicants for this position
                      </h4>
                      {apps.length === 0 ? (
                        <p className="text-gray-600 text-center py-4">No applications yet.</p>
                      ) : (
                        <div className="overflow-x-auto">
                          <table className="min-w-full text-sm bg-white rounded-lg overflow-hidden">
                            <thead className="bg-gradient-to-r from-blue-600 to-purple-600 text-white">
                              <tr>
                                <th className="p-3 text-left">Applicant ID</th>
                                <th className="p-3 text-left">Status</th>
                                <th className="p-3 text-left">Applied Date</th>
                                <th className="p-3 text-left">Actions</th>
                              </tr>
                            </thead>
                            <tbody>
                              {apps.map(a => (
                                <tr key={a.id} className="border-t hover:bg-gray-50">
                                  <td className="p-3 font-mono text-xs">{a.userId}</td>
                                  <td className="p-3">
                                    <span className={`px-2 py-1 rounded-full text-xs font-semibold ${
                                      a.status === 'Offer' ? 'bg-green-100 text-green-800' :
                                      a.status === 'Interview' ? 'bg-blue-100 text-blue-800' :
                                      a.status === 'Rejected' ? 'bg-red-100 text-red-800' :
                                      'bg-yellow-100 text-yellow-800'
                                    }`}>
                                      {a.status}
                                    </span>
                                  </td>
                                  <td className="p-3">{new Date(a.appliedAt).toLocaleDateString()}</td>
                                  <td className="p-3">
                                    <select 
                                      className="border rounded-lg p-2 text-sm focus:ring-2 focus:ring-blue-500"
                                      value={a.status}
                                      onChange={e => updateAppStatus(a.id, e.target.value)}
                                    >
                                      <option>Applied</option>
                                      <option>Reviewed</option>
                                      <option>Interview</option>
                                      <option>Offer</option>
                                      <option>Rejected</option>
                                      <option>Hired</option>
                                    </select>
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </div>
                  )}
                </>
              )}
            </div>
          ))}
          {jobs.length === 0 && (
            <div className="text-center py-12 bg-white rounded-xl shadow-md">
              <div className="text-6xl mb-4">{ICONS.job}</div>
              <p className="text-xl text-gray-600 mb-4">No job postings yet</p>
              <button 
                className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 flex items-center gap-2 mx-auto"
                onClick={() => setShowForm(true)}
              >
                <span>{ICONS.upload}</span> Create Your First Job Post
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export const JobApplicationsPage = () => {
  const [apps, setApps] = useState([]);

  const load = async () => {
    const res = await jobsApi.myApplications();
    setApps(res.data);
  };

  useEffect(() => { load(); }, []);

  const getStatusColor = (status) => {
    switch(status) {
      case 'Offer': return 'bg-green-100 text-green-800 border-green-300';
      case 'Interview': return 'bg-blue-100 text-blue-800 border-blue-300';
      case 'Reviewed': return 'bg-purple-100 text-purple-800 border-purple-300';
      case 'Rejected': return 'bg-red-100 text-red-800 border-red-300';
      case 'Hired': return 'bg-emerald-100 text-emerald-800 border-emerald-300';
      default: return 'bg-yellow-100 text-yellow-800 border-yellow-300';
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-green-50 to-blue-100 py-8">
      <div className="max-w-6xl mx-auto px-4">
        <div className="mb-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">My Applications</h1>
          <p className="text-gray-600">Track your job application status</p>
        </div>
        
        <div className="bg-white rounded-xl shadow-lg overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full">
              <thead className="bg-gradient-to-r from-blue-600 to-green-600 text-white">
                <tr>
                  <th className="p-4 text-left font-semibold">Job Title</th>
                  <th className="p-4 text-left font-semibold">Location</th>
                  <th className="p-4 text-left font-semibold">Type</th>
                  <th className="p-4 text-left font-semibold">Status</th>
                  <th className="p-4 text-left font-semibold">Applied Date</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {apps.map(a => (
                  <tr key={a.id} className="hover:bg-gray-50 transition-colors">
                    <td className="p-4 font-semibold text-gray-900">{a.jobTitle || a.jobId}</td>
                    <td className="p-4 text-gray-600">{a.jobLocation || '-'}</td>
                    <td className="p-4">
                      <span className="px-3 py-1 bg-blue-50 text-blue-700 rounded-full text-sm font-medium">
                        {a.jobType || '-'}
                      </span>
                    </td>
                    <td className="p-4">
                      <span className={`px-3 py-1 rounded-full text-sm font-semibold border ${getStatusColor(a.status)}`}>
                        {a.status}
                      </span>
                    </td>
                    <td className="p-4 text-gray-600">{new Date(a.appliedAt).toLocaleDateString()}</td>
                  </tr>
                ))}
                {apps.length === 0 && (
                  <tr>
                    <td className="p-8 text-center text-gray-500" colSpan={5}>
                      <div className="text-6xl mb-4">{ICONS.apply}</div>
                      <p className="text-xl">No applications yet. Start applying to jobs!</p>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

export const AdminUsersPage = () => {
  const [users, setUsers] = useState([]);
  const [stats, setStats] = useState(null);

  const load = async () => {
    const [usersRes, statsRes] = await Promise.all([
      adminApi.users(),
      adminApi.stats()
    ]);
    setUsers(usersRes.data);
    setStats(statsRes.data);
  };

  useEffect(() => { load(); }, []);

  const setRole = async (id, role) => {
    await adminApi.setUserRole(id, role);
    await load();
  };

  const setStatus = async (id, active) => {
    await adminApi.setUserStatus(id, active);
    await load();
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-indigo-50 to-purple-100 py-8">
      <div className="max-w-7xl mx-auto px-4">
        <div className="mb-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-2">Admin - User Management</h1>
          <p className="text-gray-600">Manage users and view platform statistics</p>
        </div>
        
        {stats && (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
            <div className="bg-gradient-to-br from-blue-500 to-blue-600 rounded-xl shadow-lg p-6 text-white">
              <div className="text-4xl font-bold mb-2">{stats.totalUsers}</div>
              <div className="text-blue-100">Total Users</div>
            </div>
            <div className="bg-gradient-to-br from-green-500 to-green-600 rounded-xl shadow-lg p-6 text-white">
              <div className="text-4xl font-bold mb-2">{stats.totalJobs}</div>
              <div className="text-green-100">Active Jobs</div>
            </div>
            <div className="bg-gradient-to-br from-purple-500 to-purple-600 rounded-xl shadow-lg p-6 text-white">
              <div className="text-4xl font-bold mb-2">{stats.totalApplications}</div>
              <div className="text-purple-100">Applications</div>
            </div>
            <div className="bg-gradient-to-br from-orange-500 to-orange-600 rounded-xl shadow-lg p-6 text-white">
              <div className="text-4xl font-bold mb-2">{stats.totalCVs}</div>
              <div className="text-orange-100">CVs Uploaded</div>
            </div>
          </div>
        )}

        <div className="bg-white rounded-xl shadow-lg overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full">
              <thead className="bg-gradient-to-r from-indigo-600 to-purple-600 text-white">
                <tr>
                  <th className="p-4 text-left font-semibold">Email</th>
                  <th className="p-4 text-left font-semibold">Name</th>
                  <th className="p-4 text-left font-semibold">Role</th>
                  <th className="p-4 text-left font-semibold">Status</th>
                  <th className="p-4 text-left font-semibold">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {users.map(u => (
                  <tr key={u.id} className="hover:bg-gray-50">
                    <td className="p-4 text-gray-900">{u.email}</td>
                    <td className="p-4 text-gray-700">{u.firstName} {u.lastName}</td>
                    <td className="p-4">
                      <span className={`px-3 py-1 rounded-full text-xs font-semibold ${
                        u.role === 'Admin' ? 'bg-red-100 text-red-800' :
                        u.role === 'Recruiter' ? 'bg-purple-100 text-purple-800' :
                        'bg-blue-100 text-blue-800'
                      }`}>
                        {u.role}
                      </span>
                    </td>
                    <td className="p-4">
                      <span className={`px-3 py-1 rounded-full text-xs font-semibold ${
                        u.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                      }`}>
                        {u.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="p-4 flex gap-2">
                      <select 
                        className="border rounded-lg p-2 text-sm focus:ring-2 focus:ring-purple-500" 
                        value={u.role} 
                        onChange={e=>setRole(u.id, e.target.value)}
                      >
                        <option>Student</option>
                        <option>Recruiter</option>
                        <option>Admin</option>
                      </select>
                      <button 
                        className={`px-4 py-2 rounded-lg text-sm font-medium ${
                          u.isActive ? 'bg-red-100 text-red-700 hover:bg-red-200' : 'bg-green-100 text-green-700 hover:bg-green-200'
                        }`}
                        onClick={()=>setStatus(u.id, !u.isActive)}
                      >
                        {u.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};
