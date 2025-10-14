import React, { useEffect, useState, useCallback } from 'react';
import { directoryApi } from '../services/directoryApi';
import ICONS from '../utils/icons';

export const StudentDirectoryPage = () => {
  const [q, setQ] = useState('');
  const [skill, setSkill] = useState('');
  const [students, setStudents] = useState([]);
  const [selected, setSelected] = useState(null);
  const [cvs, setCvs] = useState([]);

  const search = useCallback(async () => {
    const res = await directoryApi.students(q, skill);
    setStudents(res.data);
  }, [q, skill]);

  useEffect(() => { search(); }, [search]);

  const open = async (s) => {
    setSelected(s);
    const res = await directoryApi.studentCvs(s.id);
    setCvs(res.data);
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-purple-50 to-blue-50 py-8">
      <div className="max-w-7xl mx-auto px-4">
        <div className="mb-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-2 flex items-center gap-2">{ICONS.students} Student Directory</h1>
          <p className="text-gray-600">Search for students by name, email, or skills</p>
        </div>
        
        <div className="bg-white rounded-xl shadow-md p-6 mb-6">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <input 
              className="col-span-2 border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-purple-500 focus:border-transparent" 
              placeholder={`${ICONS.search} Search by name or email...`} 
              value={q} 
              onChange={e=>setQ(e.target.value)} 
            />
            <input 
              className="border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-purple-500 focus:border-transparent" 
              placeholder={`${ICONS.skills} Filter by skill`} 
              value={skill} 
              onChange={e=>setSkill(e.target.value)} 
            />
            <button 
              className="px-6 py-3 bg-gradient-to-r from-purple-600 to-blue-600 text-white rounded-lg hover:shadow-lg transition-shadow font-medium flex items-center justify-center gap-2" 
              onClick={search}
            >
              <span>{ICONS.search}</span> Search
            </button>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {students.map(s => (
            <div 
              key={s.id} 
              className="bg-white rounded-xl shadow-md hover:shadow-2xl transition-all p-6 cursor-pointer border-l-4 border-purple-500 transform hover:-translate-y-1"
              onClick={()=>open(s)}
            >
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h3 className="text-xl font-bold text-gray-900 flex items-center gap-2">{ICONS.user} {s.firstName} {s.lastName}</h3>
                  <p className="text-sm text-gray-600 mt-1">{ICONS.email} {s.email}</p>
                </div>
                <div className="w-12 h-12 bg-gradient-to-br from-purple-400 to-blue-500 rounded-full flex items-center justify-center text-white font-bold text-lg">
                  {s.firstName?.charAt(0)}{s.lastName?.charAt(0)}
                </div>
              </div>
              
              {s.profile?.skills && (
                <div className="mb-3">
                  <p className="text-xs font-semibold text-gray-700 mb-2 flex items-center gap-1">{ICONS.skills} Skills:</p>
                  <div className="flex flex-wrap gap-1">
                    {s.profile.skills.split(',').slice(0, 4).map((skill, i) => (
                      <span key={i} className="px-2 py-1 bg-purple-100 text-purple-700 rounded-md text-xs font-medium">
                        {skill.trim()}
                      </span>
                    ))}
                  </div>
                </div>
              )}
              
              {s.latestCV && (
                <div className="mt-4 pt-4 border-t border-gray-200">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-xs font-semibold text-gray-700 flex items-center gap-1">{ICONS.document} Latest CV</p>
                      <p className="text-xs text-gray-600 mt-1 truncate">{s.latestCV.fileName}</p>
                    </div>
                    <div className="text-right">
                      <div className="text-lg font-bold text-purple-700">
                        {Math.round(((s.latestCV.interactive?.overallScore ?? s.latestCV.basicFeedback?.qualityScore) ?? 0) * 100)}%
                      </div>
                      <p className="text-xs text-gray-500">Score</p>
                    </div>
                  </div>
                </div>
              )}
              
              <div className="mt-4">
                <button className="w-full px-4 py-2 bg-gradient-to-r from-purple-600 to-blue-600 text-white rounded-lg hover:shadow-md transition-shadow text-sm font-medium flex items-center justify-center gap-2">
                  <span>{ICONS.user}</span> View Profile
                </button>
              </div>
            </div>
          ))}
          {students.length === 0 && (
            <div className="col-span-3 text-center py-16 bg-white rounded-xl shadow-md">
              <div className="text-7xl mb-4">{ICONS.search}</div>
              <p className="text-2xl text-gray-700 font-semibold mb-2">No students found</p>
              <p className="text-gray-500">Try adjusting your search criteria</p>
            </div>
          )}
        </div>

        {/* Student Detail Modal */}
        {selected && (
          <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50" onClick={()=>{setSelected(null); setCvs([])}}>
            <div className="bg-white rounded-2xl shadow-2xl max-w-5xl w-full max-h-[90vh] overflow-auto" onClick={e=>e.stopPropagation()}>
              <div className="sticky top-0 bg-gradient-to-r from-purple-600 to-blue-600 text-white p-6 rounded-t-2xl">
                <div className="flex justify-between items-center">
                  <div className="flex items-center gap-4">
                    <div className="w-16 h-16 bg-white bg-opacity-20 rounded-full flex items-center justify-center text-2xl font-bold">
                      {selected.firstName?.charAt(0)}{selected.lastName?.charAt(0)}
                    </div>
                    <div>
                      <h3 className="text-3xl font-bold flex items-center gap-2">{ICONS.user} {selected.firstName} {selected.lastName}</h3>
                      <p className="text-purple-100 mt-1">{ICONS.email} {selected.email}</p>
                    </div>
                  </div>
                  <button 
                    className="px-4 py-2 bg-white bg-opacity-20 hover:bg-opacity-30 rounded-lg transition-colors font-medium flex items-center gap-2" 
                    onClick={()=>{setSelected(null); setCvs([])}}
                  >
                    <span>?</span> Close
                  </button>
                </div>
              </div>
              
              <div className="p-6">
                <h4 className="text-2xl font-bold mb-6 text-gray-900 flex items-center gap-2">
                  <span>{ICONS.calendar}</span> CV History
                </h4>
                <div className="overflow-x-auto">
                  <table className="min-w-full">
                    <thead className="bg-gradient-to-r from-purple-100 to-blue-100">
                      <tr>
                        <th className="p-4 text-left font-semibold text-gray-900">File Name</th>
                        <th className="p-4 text-left font-semibold text-gray-900">Uploaded</th>
                        <th className="p-4 text-left font-semibold text-gray-900">Basic Score</th>
                        <th className="p-4 text-left font-semibold text-gray-900">Interactive Score</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {cvs.map(c => (
                        <tr key={c.id} className="hover:bg-gray-50 transition-colors">
                          <td className="p-4">
                            <div className="flex items-center gap-2">
                              <span className="text-2xl">{ICONS.document}</span>
                              <div>
                                <p className="font-medium text-gray-900">{c.fileName}</p>
                                {c.isActive && <span className="text-xs px-2 py-1 bg-green-100 text-green-700 rounded-full font-semibold flex items-center gap-1">{ICONS.approved} Current</span>}
                              </div>
                            </div>
                          </td>
                          <td className="p-4 text-gray-600">{new Date(c.uploadedAt).toLocaleDateString('en-US',{year:'numeric',month:'short',day:'numeric'})}</td>
                          <td className="p-4">
                            {c.basicFeedback ? (
                              <div className="flex items-center gap-2">
                                <div className="w-16 h-16 rounded-full bg-gradient-to-br from-blue-400 to-blue-600 flex items-center justify-center text-white font-bold text-lg">{Math.round(c.basicFeedback.qualityScore*100)}%</div>
                                {c.basicFeedback.isApproved && <span className="text-green-600 text-2xl">{ICONS.approved}</span>}
                              </div>
                            ) : <span className="text-gray-400">-</span>}
                          </td>
                          <td className="p-4">
                            {c.interactive ? (
                              <div className="flex items-center gap-2">
                                <div className="w-16 h-16 rounded-full bg-gradient-to-br from-purple-400 to-purple-600 flex items-center justify-center text-white font-bold text-lg">{Math.round(c.interactive.overallScore*100)}%</div>
                                {c.interactive.isApproved && <span className="text-green-600 text-2xl">{ICONS.approved}</span>}
                              </div>
                            ) : <span className="text-gray-400">-</span>}
                          </td>
                        </tr>
                      ))}
                      {cvs.length === 0 && (
                        <tr>
                          <td className="p-8 text-center text-gray-500" colSpan={4}>
                            <div className="text-5xl mb-2">{ICONS.document}</div>
                            <p>No CVs uploaded yet</p>
                          </td>
                        </tr>
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
