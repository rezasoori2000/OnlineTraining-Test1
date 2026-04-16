import { } from 'react';
import { FolderPanel } from '../App';
import { CourseList } from './CourseList';
import { FolderTree } from './FolderTree';
import { useFolderTree, useFolderDetail } from '../hooks/usePortalData';

interface MainPageProps {
  selectedFolderId: string | null
  onFolderSelect: (id: string | null) => void
  onCourseOpen: (id: string, title: string) => void
}

export function MainPage({ selectedFolderId, onFolderSelect, onCourseOpen }: MainPageProps) {

    const { data: tree, isLoading: treeLoading } = useFolderTree();
    const { data: folder } = useFolderDetail(selectedFolderId);

    return (
        <div className="h-full flex flex-col">
            {/* Top bar */}
            <header className="h-12 bg-brand-950 flex items-center px-4 shrink-0">
                <h1 className="text-white font-semibold text-base tracking-wide">PGL WIKI</h1>
            </header>

            <div className="flex flex-1 min-h-0">
                {/* Left column */}
                <aside className="w-52 border-r border-gray-200 bg-white shrink-0 flex flex-col">
                    {/* Folders panel — top half */}
                    <div className="flex flex-col border-b border-gray-200" style={{ height: '50%' }}>
                        <div className="px-3 py-2 border-b border-gray-100 shrink-0">
                            <span className="text-xs font-bold text-brand-700 uppercase tracking-wider">Folders</span>
                        </div>
                        <div className="flex-1 overflow-y-auto p-1.5">
                            {treeLoading && <div className="text-xs text-gray-400 p-2">Loading...</div>}
                            {tree && (
                                <FolderTree
                                    nodes={tree}
                                    selectedId={selectedFolderId}
                                    onSelect={onFolderSelect} />
                            )}
                        </div>
                    </div>

                    {/* Courses panel — bottom half */}
                    <div className="flex flex-col flex-1 min-h-0">
                        <div className="px-3 py-2 border-b border-gray-100 shrink-0">
                            <span className="text-xs font-bold text-brand-700 uppercase tracking-wider">Courses</span>
                        </div>
                        <div className="flex-1 overflow-y-auto">
                            {folder ? (
                                <CourseList
                                    courses={folder.courses}
                                    selectedCourseId={null}
                                    onSelect={(id) => {
                                        const title = folder.courses.find(c => c.courseId === id)?.title ?? '';
                                        onCourseOpen(id, title);
                                    }} />
                            ) : (
                                <div className="p-3 text-xs text-gray-400 italic">Select a folder</div>
                            )}
                        </div>
                    </div>
                </aside>

                {/* Right content area */}
                <main className="flex-1 flex flex-col min-h-0 bg-white">
                    {!selectedFolderId && (
                        <div className="flex items-center justify-center h-full text-gray-400 text-sm">
                            Select a folder to get started
                        </div>
                    )}
                    {selectedFolderId && folder && (
                        <FolderPanel folder={folder} />
                    )}
                </main>
            </div>
        </div>
    );
}
