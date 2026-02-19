import React, { useEffect, useState } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'

type Project = { id: string; name: string }
type WbsElement = { id: string; code: string; name: string; workType: string; systemDiscipline: string }
type WorkPackage = { id: string; wbsElementId: string; name: string; status: string }
type ScheduleActivity = { id: string; executionPackageId: string; name: string; plannedStart: string; plannedFinish: string; duration: number; float: number; isCritical: boolean }

const roles = ['Investor','ProjectDirector','TechnicalCustomer','CommissioningManager','AssetManager']
const tabs = ['Dashboard','WBS Explorer','Packages','Schedule','Decisions','Integration']

function App() {
  const [project, setProject] = useState<Project | null>(null)
  const [tab, setTab] = useState(tabs[0])
  const [role, setRole] = useState(roles[0])
  const [data, setData] = useState<any>({})
  const [ai, setAi] = useState('')
  const [selectedPackageId, setSelectedPackageId] = useState<string | null>(null)

  useEffect(() => {
    fetch('http://localhost:8080/api/projects').then(r => r.json()).then((projects: Project[]) => setProject(projects[0]))
  }, [])

  useEffect(() => {
    if (!project) return
    Promise.all([
      fetch(`http://localhost:8080/api/dashboard/${project.id}`).then(r => r.json()),
      fetch(`http://localhost:8080/api/wbs/${project.id}`).then(r => r.json()),
      fetch(`http://localhost:8080/api/packages/${project.id}`).then(r => r.json()),
      fetch(`http://localhost:8080/api/schedule/${project.id}`).then(r => r.json()),
      fetch(`http://localhost:8080/api/decisions/${project.id}`).then(r => r.json()),
      fetch(`http://localhost:8080/api/integration/${project.id}`).then(r => r.json())
    ]).then(([dashboard,wbs,packages,schedule,decisions,integration]) => {
      setData({dashboard,wbs,packages,schedule,decisions,integration})
      setSelectedPackageId(packages[0]?.id ?? null)
    })
  }, [project])

  const analyze = async () => {
    if (!project) return
    const res = await fetch(`http://localhost:8080/api/ai/analyze?projectId=${project.id}`, { method: 'POST' })
    const body = await res.json()
    setAi(body.report)
  }

  return <div className="page">
    <h1>Developer Platform MVP</h1>
    <div className="bar">
      <label>Role: <select value={role} onChange={e => setRole(e.target.value)}>{roles.map(r => <option key={r}>{r}</option>)}</select></label>
      <span>Project: {project?.name ?? 'loading...'}</span>
      <button onClick={analyze}>Run AI analysis</button>
    </div>
    <div className="tabs">{tabs.map(t => <button key={t} onClick={() => setTab(t)} className={tab===t?'active':''}>{t}</button>)}</div>

    {tab === 'Dashboard' && <section>
      <h3>Critical Path / Blocked / Scenario Readiness</h3>
      <pre>{JSON.stringify(data.dashboard, null, 2)}</pre>
      <h3>AI report</h3>
      <p>{ai || 'No report yet'}</p>
    </section>}

    {tab === 'WBS Explorer' && <Table rows={data.wbs} />}
    {tab === 'Packages' && <Table rows={data.packages} />}
    {tab === 'Schedule' && <Table rows={data.schedule} />}
    {tab === 'Decisions' && <Table rows={data.decisions} />}
    {tab === 'Integration' && <Table rows={data.integration} />}

    {role === 'ProjectDirector' && <ProjectDirectorPanel
      wbs={data.wbs}
      packages={data.packages}
      schedule={data.schedule}
      selectedPackageId={selectedPackageId}
      onSelectPackage={setSelectedPackageId}
      onOpenSchedule={() => setTab('Schedule')}
    />}
  </div>
}

function ProjectDirectorPanel({
  wbs = [] as WbsElement[],
  packages = [] as WorkPackage[],
  schedule = [] as ScheduleActivity[],
  selectedPackageId,
  onSelectPackage,
  onOpenSchedule
}: {
  wbs?: WbsElement[]
  packages?: WorkPackage[]
  schedule?: ScheduleActivity[]
  selectedPackageId: string | null
  onSelectPackage: (id: string) => void
  onOpenSchedule: () => void
}) {
  if (!wbs.length && !packages.length) return null

  const selectedPackage = packages.find(p => p.id === selectedPackageId) ?? packages[0]
  const packageSchedule = schedule.filter(a => a.executionPackageId === selectedPackage?.id)

  return <section className="director-panel">
    <h2>Project Director workspace</h2>
    <p>WBS and work packages with a direct transition to the project schedule.</p>

    <div className="director-grid">
      <div>
        <h3>WBS</h3>
        <Table rows={wbs} />
      </div>

      <div>
        <h3>Work packages</h3>
        {packages.length ? <ul className="package-list">
          {packages.map(pkg => (
            <li key={pkg.id}>
              <button
                className={selectedPackage?.id === pkg.id ? 'active' : ''}
                onClick={() => onSelectPackage(pkg.id)}
              >
                {pkg.name} ({pkg.status})
              </button>
            </li>
          ))}
        </ul> : <p>empty</p>}

        {selectedPackage && <div className="package-relations">
          <h4>Package-schedule relation</h4>
          <p>
            Package <b>{selectedPackage.name}</b> is connected to {packageSchedule.length} schedule activities.
          </p>
          <button onClick={onOpenSchedule}>Open full schedule</button>
          <h4>Schedule activities for selected package</h4>
          <Table rows={packageSchedule} />
        </div>}
      </div>
    </div>
  </section>
}

function Table({ rows = [] as any[] }) {
  if (!rows.length) return <p>empty</p>
  const keys = Object.keys(rows[0])
  return <table><thead><tr>{keys.map(k => <th key={k}>{k}</th>)}</tr></thead><tbody>{rows.map((r, i) => <tr key={i}>{keys.map(k => <td key={k}>{String(r[k])}</td>)}</tr>)}</tbody></table>
}

createRoot(document.getElementById('root')!).render(<React.StrictMode><App /></React.StrictMode>)
