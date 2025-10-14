import React, { useEffect, useState, useCallback } from 'react';
import { superAdminApi } from '../services/adminApiExtended';
import ICONS from '../utils/icons';

export const AdminDirectoryPage = () => {
  const [q, setQ] = useState('');
  const [students, setStudents] = useState([]);
  const [recruiters, setRecruiters] = useState([]);
  const [jobs, setJobs] = useState([]);
  const [selectedStudent, setSelectedStudent] = useState(null);
  const [studentCvs, setStudentCvs] = useState([]);
  const [selectedRecruiter, setSelectedRecruiter] = useState(null);
  const [recruiterJobs, setRecruiterJobs] = useState([]);
  const [activeTab, setActiveTab] = useState('students');

  const searchAll = useCallback(async () => {
    const [s, r, j] = await Promise.all([
      superAdminApi.students(q),
      superAdminApi.recruiters(q),
      superAdminApi.allJobs(q)
    ]);
    setStudents(s.data);
    setRecruiters(r.data);
    setJobs(j.data);
  }, [q]);

  useEffect(() => { searchAll(); }, [searchAll]);

  const openStudent = async (s) => {
    setSelectedStudent(s);
    const res = await superAdminApi.studentCvs(s.id);
    setStudentCvs(res.data);
  };

  const openRecruiter = async (r) => {
    setSelectedRecruiter(r);
    const res = await superAdminApi.recruiterJobs(r.id);
    setRecruiterJobs(res.data);
  };

  const downloadCv = async (cvId, fileName) => {
    const res = await superAdminApi.downloadCv(cvId);
    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', fileName || 'cv');
    document.body.appendChild(link);
    link.click();
    link.remove();
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-blue-50 py-8">
      <div className="max-w-7xl mx-auto px-4">
        <div className="mb-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-2 flex items-center gap-2">{ICONS.admin} Admin Directory</h1>
          <p className="text-gray-600">Manage users, monitor activity, and review job postings</p>
        </div>
        
        <div className="bg-white rounded-xl shadow-md p-6 mb-6">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
            <input 
              className="flex-1 border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-indigo-500 focus:border-transparent" 
              placeholder={`${ICONS.search} Search students, recruiters, or jobs...`} 
              value={q} 
              onChange={e=>setQ(e.target.value)} 
            />
            <div className="flex gap-3">
              <button 
                onClick={() => setActiveTab('students')} 
                className={`px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2 ${activeTab==='students'?'bg-indigo-600 text-white':'bg-gray-100 text-gray-700 hover:bg-gray-200'}`}
              >
                {ICONS.students} Students ({students.length})
              </button>
              <button 
                onClick={() => setActiveTab('recruiters')} 
                className={`px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2 ${activeTab==='recruiters'?'bg-purple-600 text-white':'bg-gray-100 text-gray-700 hover:bg-gray-200'}`}
              >
                {ICONS.recruiter} Recruiters ({recruiters.length})
              </button>
              <button 
                onClick={() => setActiveTab('jobs')} 
                className={`px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2 ${activeTab==='jobs'?'bg-blue-600 text-white':'bg-gray-100 text-gray-700 hover:bg-gray-200'}`}
              >
                {ICONS.job} Jobs ({jobs.length})
              </button>
            </div>
          </div>
        </div>

        {/* Students Tab */}
        {activeTab === 'students' && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {students.map(s => (
              <div key={s.id} className="bg-white rounded-xl shadow-md p-6 hover:shadow-lg transition cursor-pointer border-l-4 border-indigo-500" onClick={()=>openStudent(s)}>
                <div className="flex items-center gap-3 mb-3">
                  <div className="w-12 h-12 rounded-full bg-gradient-to-br from-indigo-400 to-blue-500 flex items-center justify-center text-white font-semibold text-lg">{s.firstName?.charAt(0)}{s.lastName?.charAt(0)}</div>
                  <div>
                    <p className="font-semibold text-gray-900 flex items-center gap-2">{ICONS.user} {s.firstName} {s.lastName}</p>
                    <p className="text-sm text-gray-600 flex items-center gap-1">{ICONS.email} {s.email}</p>
                  </div>
                </div>
                <div className="text-xs text-gray-500 flex items-center gap-1">{ICONS.calendar} Joined {new Date(s.createdAt).toLocaleDateString()}</div>
              </div>
            ))}
            {students.length === 0 && (
              <div className="col-span-3 text-center p-12 bg-white rounded-xl shadow">
                <div className="text-6xl mb-4">{ICONS.sad}</div>
                <p className="text-xl text-gray-600">No students found</p>
              </div>
            )}
          </div>
        )}

        {/* Recruiters Tab */}
        {activeTab === 'recruiters' && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {recruiters.map(r => (
              <div key={r.id} className="bg-white rounded-xl shadow-md p-6 hover:shadow-lg transition cursor-pointer border-l-4 border-purple-500" onClick={()=>openRecruiter(r)}>
                <div className="flex items-center gap-3 mb-3">
                  <div className="w-12 h-12 rounded-full bg-gradient-to-br from-purple-400 to-pink-500 flex items-center justify-center text-white font-semibold text-lg">{r.firstName?.charAt(0)}{r.lastName?.charAt(0)}</div>
                  <div>
                    <p className="font-semibold text-gray-900 flex items-center gap-2">{ICONS.recruiter} {r.firstName} {r.lastName}</p>
                    <p className="text-sm text-gray-600 flex items-center gap-1">{ICONS.email} {r.email}</p>
                  </div>
                </div>
                <div className="text-xs text-gray-500 flex items-center gap-1">{ICONS.calendar} Joined {new Date(r.createdAt).toLocaleDateString()}</div>
              </div>
            ))}
            {recruiters.length === 0 && (
              <div className="col-span-3 text-center p-12 bg-white rounded-xl shadow">
                <div className="text-6xl mb-4">{ICONS.sad}</div>
                <p className="text-xl text-gray-600">No recruiters found</p>
              </div>
            )}
          </div>
        )}

        {/* Jobs Tab */}
        {activeTab === 'jobs' && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {jobs.map(j => (
              <div key={j.id} className="bg-white rounded-xl shadow-md p-6 hover:shadow-lg transition border-l-4 border-blue-500">
                <h3 className="font-semibold text-gray-900 mb-1 flex items-center gap-2">{ICONS.job} {j.title}</h3>
                <div className="text-sm text-gray-600 flex flex-col gap-1 mt-2">
                  <span>{ICONS.location} {j.location}</span>
                  <span>{ICONS.calendar} {j.jobType}</span>
                  <span>{ICONS.recruiter} {j.recruiter?.email}</span>
                </div>
                <div className="mt-3 flex items-center gap-2 text-xs text-gray-500">
                  <span>{ICONS.calendar} Posted {new Date(j.createdAt).toLocaleDateString()}</span>
                </div>
              </div>
            ))}
            {jobs.length === 0 && (
              <div className="col-span-3 text-center p-12 bg-white rounded-xl shadow">
                <div className="text-6xl mb-4">{ICONS.sad}</div>
                <p className="text-xl text-gray-600">No jobs found</p>
              </div>
            )}
          </div>
        )}

        {/* Student Details Modal */}
        {selectedStudent && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50" onClick={()=>{setSelectedStudent(null); setStudentCvs([])}}>
            <div className="bg-white rounded-2xl shadow-2xl max-w-4xl w-full max-h-[90vh] overflow-auto" onClick={e=>e.stopPropagation()}>
              <div className="sticky top-0 bg-gradient-to-r from-indigo-600 to-blue-600 text-white p-6 rounded-t-2xl">
                <div className="flex justify-between items-center">
                  <div>
                    <h3 className="text-2xl font-bold flex items-center gap-2">{ICONS.user} {selectedStudent.firstName} {selectedStudent.lastName}</h3>
                    <p className="text-indigo-100 flex items-center gap-1">{ICONS.email} {selectedStudent.email}</p>
                  </div>
                  <button className="px-4 py-2 bg-white bg-opacity-20 hover:bg-opacity-30 rounded-lg transition-colors flex items-center gap-2" onClick={()=>{setSelectedStudent(null); setStudentCvs([])}}>
                    <span>?</span> Close
                  </button>
                </div>
              </div>
              <div className="p-6">
                <h4 className="text-xl font-bold mb-4 text-gray-900 flex items-center gap-2">{ICONS.calendar} CV History</h4>
                <div className="overflow-x-auto">
                  <table className="min-w-full text-sm">
                    <thead className="bg-gray-100">
                      <tr>
                        <th className="p-3 text-left font-semibold">File</th>
                        <th className="p-3 text-left font-semibold">Uploaded</th>
                        <th className="p-3 text-left font-semibold">Score</th>
                        <th className="p-3 text-left font-semibold">Action</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {studentCvs.map(c => (
                        <tr key={c.id} className="hover:bg-gray-50">
                          <td className="p-3 flex items-center gap-2">
                            <span className="text-xl">{ICONS.document}</span>
                            <span className="font-medium text-gray-800">{c.fileName}</span>
                          </td>
                          <td className="p-3 text-gray-600">{new Date(c.uploadedAt).toLocaleDateString()}</td>
                          <td className="p-3">
                            {c.qualityScore ? (
                              <span className={`px-2 py-1 rounded-full text-xs font-semibold ${c.qualityScore>=0.8?'bg-green-100 text-green-800':c.qualityScore>=0.6?'bg-yellow-100 text-yellow-800':'bg-red-100 text-red-800'}`}>{Math.round(c.qualityScore*100)}%</span>
                            ) : <span className="text-gray-400">-</span>}
                          </td>
                          <td className="p-3">
                            <button 
                              className="px-3 py-1 bg-gray-200 hover:bg-gray-300 rounded text-xs flex items-center gap-1" 
                              onClick={()=>downloadCv(c.id, c.fileName)}
                            >
                              {ICONS.download} Download
                            </button>
                          </td>
                        </tr>
                      ))}
                      {studentCvs.length === 0 && (
                        <tr><td className="p-4 text-center text-gray-500" colSpan={4}>No CVs uploaded yet</td></tr>
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Recruiter Details Modal */}
        {selectedRecruiter && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50" onClick={()=>{setSelectedRecruiter(null); setRecruiterJobs([])}}>
            <div className="bg-white rounded-2xl shadow-2xl max-w-4xl w-full max-h-[90vh] overflow-auto" onClick={e=>e.stopPropagation()}>
              <div className="sticky top-0 bg-gradient-to-r from-purple-600 to-pink-600 text-white p-6 rounded-t-2xl">
                <div className="flex justify-between items-center">
                  <div>
                    <h3 className="text-2xl font-bold flex items-center gap-2">{ICONS.recruiter} {selectedRecruiter.firstName} {selectedRecruiter.lastName}</h3>
                    <p className="text-purple-100 flex items-center gap-1">{ICONS.email} {selectedRecruiter.email}</p>
                  </div>
                  <button className="px-4 py-2 bg-white bg-opacity-20 hover:bg-opacity-30 rounded-lg transition-colors flex items-center gap-2" onClick={()=>{setSelectedRecruiter(null); setRecruiterJobs([])}}>
                    <span>?</span> Close
                  </button>
                </div>
              </div>
              <div className="p-6">
                <h4 className="text-xl font-bold mb-4 text-gray-900 flex items-center gap-2">{ICONS.job} Job Postings</h4>
                <div className="overflow-x-auto">
                  <table className="min-w-full text-sm">
                    <thead className="bg-gray-100">
                      <tr>
                        <th className="p-3 text-left font-semibold">Title</th>
                        <th className="p-3 text-left font-semibold">Location</th>
                        <th className="p-3 text-left font-semibold">Type</th>
                        <th className="p-3 text-left font-semibold">Status</th>
                        <th className="p-3 text-left font-semibold">Posted</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {recruiterJobs.map(j => (
                        <tr key={j.id} className="hover:bg-gray-50">
                          <td className="p-3 font-medium">{j.title}</td>
                          <td className="p-3 text-gray-600">{j.location}</td>
                          <td className="p-3"><span className="px-2 py-1 bg-blue-100 text-blue-800 rounded-full text-xs font-medium">{j.jobType}</span></td>
                          <td className="p-3"><span className={`px-2 py-1 rounded-full text-xs font-semibold ${j.isActive?'bg-green-100 text-green-800':'bg-gray-100 text-gray-600'}`}>{j.isActive?'Active':'Closed'}</span></td>
                          <td className="p-3 text-gray-600">{new Date(j.createdAt).toLocaleDateString()}</td>
                        </tr>
                      ))}
                      {recruiterJobs.length === 0 && (
                        <tr><td className="p-4 text-center text-gray-500" colSpan={5}>No job postings yet</td></tr>
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
