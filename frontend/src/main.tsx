import React, { useEffect, useMemo, useState } from 'react'
import { createRoot } from 'react-dom/client'
import './styles.css'

type Project = { id: string; name: string }
type WbsElement = { id: string; code: string; name: string; workType: string; systemDiscipline: string }
type WorkPackage = { id: string; wbsElementId: string; name: string; status: string }
type ScheduleActivity = { id: string; executionPackageId: string; name: string; plannedStart: string; plannedFinish: string; duration: number; float: number; isCritical: boolean }

type TabKey = 'dashboard' | 'wbs' | 'packages' | 'schedule' | 'decisions' | 'integration'

const roles = [
  { value: 'Investor', label: 'Инвестор' },
  { value: 'ProjectDirector', label: 'Директор проекта' },
  { value: 'TechnicalCustomer', label: 'Технический заказчик' },
  { value: 'CommissioningManager', label: 'Менеджер ПНР' },
  { value: 'AssetManager', label: 'Менеджер эксплуатации' }
]

const tabs: Array<{ key: TabKey; label: string }> = [
  { key: 'dashboard', label: 'Панель управления' },
  { key: 'wbs', label: 'Структура работ (WBS)' },
  { key: 'packages', label: 'Пакеты работ' },
  { key: 'schedule', label: 'График' },
  { key: 'decisions', label: 'Решения' },
  { key: 'integration', label: 'Интеграции' }
]

const fieldLabels: Record<string, string> = {
  id: 'ID',
  projectId: 'ID проекта',
  parentId: 'ID родителя',
  name: 'Название',
  code: 'Код',
  workType: 'Тип работ',
  systemDiscipline: 'Системная дисциплина',
  wbsElementId: 'ID элемента WBS',
  status: 'Статус',
  executionPackageId: 'ID пакета исполнения',
  decisionBlocked: 'Блокируется решением',
  integrationRequired: 'Требуется интеграция',
  activities: 'Активности',
  decisions: 'Решения',
  title: 'Название',
  createdAt: 'Создано',
  approvedAt: 'Согласовано',
  plannedStart: 'Плановое начало',
  plannedFinish: 'Плановое окончание',
  duration: 'Длительность',
  float: 'Резерв времени',
  isCritical: 'Критический путь',
  blockedItems: 'Блокирующие элементы',
  scenarioReadiness: 'Готовность сценариев',
  criticalPathLength: 'Длина критического пути'
}

const valueTranslations: Record<string, string> = {
  // WBS
  Design: 'Проектирование',
  Procurement: 'Закупка',
  Construction: 'Строительство',
  HVAC: 'ОВиК',
  Electrical: 'Электроснабжение',
  'Access Control': 'Контроль доступа',
  Commissioning: 'Пусконаладка',
  Management: 'Управление',
  // Integration
  'Core systems startup': 'Запуск базовых систем',
  'Security integration': 'Интеграция систем безопасности',
  // Work packages
  Planned: 'Запланирован',
  InProgress: 'В работе',
  decisionBlocked: 'Блокируется решением',
  integrationRequired: 'Требуется интеграция',
  activities: 'Активности',
  decisions: 'Решения',
  true: 'Да',
  false: 'Нет'
}

const hiddenFieldsByTab: Partial<Record<TabKey, string[]>> = {
  wbs: ['id', 'projectId', 'parentId'],
  schedule: ['id', 'executionPackageId'],
  decisions: ['id', 'executionPackageId'],
  integration: ['id']
}

function App() {
  const [project, setProject] = useState<Project | null>(null)
  const [tab, setTab] = useState<TabKey>(tabs[0].key)
  const [role, setRole] = useState(roles[0].value)
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
    ]).then(([dashboard, wbs, packages, schedule, decisions, integration]) => {
      setData({ dashboard, wbs, packages, schedule, decisions, integration })
      setSelectedPackageId(packages[0]?.id ?? null)
    })
  }, [project])

  const analyze = async () => {
    if (!project) return
    const res = await fetch(`http://localhost:8080/api/ai/analyze?projectId=${project.id}`, { method: 'POST' })
    const body = await res.json()
    setAi(body.report)
  }

  const projectLabel = useMemo(() => project?.name ?? 'Загрузка...', [project])

  return <div className="page">
    <header className="header-card">
      <h1>Цифровая платформа управления проектом</h1>
      <p>Единое пространство для контроля структуры работ, сроков, решений и интеграций.</p>
    </header>

    <div className="bar card">
      <label>Роль:
        <select value={role} onChange={e => setRole(e.target.value)}>
          {roles.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
        </select>
      </label>
      <span>Проект: <b>{projectLabel}</b></span>
      <button onClick={analyze}>Запустить AI-анализ</button>
    </div>

    <div className="tabs">
      {tabs.map(t => <button key={t.key} onClick={() => setTab(t.key)} className={tab === t.key ? 'active' : ''}>{t.label}</button>)}
    </div>

    {tab === 'dashboard' && <section className="card">
      <h3>Критический путь / Блокировки / Готовность сценариев</h3>
      <pre>{JSON.stringify(data.dashboard, null, 2)}</pre>
      <h3>Отчет AI</h3>
      <p>{ai || 'Отчет пока не сформирован.'}</p>
    </section>}

    {tab === 'wbs' && <section className="card"><Table rows={data.wbs} hiddenFields={hiddenFieldsByTab.wbs} /></section>}
    {tab === 'packages' && <section className="card"><Table rows={data.packages} /></section>}
    {tab === 'schedule' && <section className="card"><Table rows={data.schedule} hiddenFields={hiddenFieldsByTab.schedule} /></section>}
    {tab === 'decisions' && <section className="card"><Table rows={data.decisions} hiddenFields={hiddenFieldsByTab.decisions} /></section>}
    {tab === 'integration' && <section className="card"><Table rows={data.integration} hiddenFields={hiddenFieldsByTab.integration} /></section>}

    {role === 'ProjectDirector' && <ProjectDirectorPanel
      wbs={data.wbs}
      packages={data.packages}
      schedule={data.schedule}
      selectedPackageId={selectedPackageId}
      onSelectPackage={setSelectedPackageId}
      onOpenSchedule={() => setTab('schedule')}
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

  return <section className="director-panel card">
    <h2>Рабочее место директора проекта</h2>
    <p>Связь структуры работ и пакетов исполнения с оперативным переходом в календарный график.</p>

    <div className="director-grid">
      <div>
        <h3>Структура работ (WBS)</h3>
        <Table rows={wbs} />
      </div>

      <div>
        <h3>Пакеты работ</h3>
        {packages.length ? <ul className="package-list">
          {packages.map(pkg => (
            <li key={pkg.id}>
              <button
                className={selectedPackage?.id === pkg.id ? 'active' : ''}
                onClick={() => onSelectPackage(pkg.id)}
              >
                {translateValue(pkg.name)} ({translateValue(pkg.status)})
              </button>
            </li>
          ))}
        </ul> : <p>Пусто</p>}

        {selectedPackage && <div className="package-relations">
          <h4>Связь пакета с графиком</h4>
          <p>
            Пакет <b>{selectedPackage.name}</b> связан с {packageSchedule.length} активностями графика.
          </p>
          <button onClick={onOpenSchedule}>Открыть полный график</button>
          <h4>Активности графика выбранного пакета</h4>
          <Table rows={packageSchedule} hiddenFields={hiddenFieldsByTab.schedule} />
        </div>}
      </div>
    </div>
  </section>
}

function translateValue(value: unknown) {
  return valueTranslations[String(value)] ?? String(value)
}

function Table({ rows = [] as any[], hiddenFields = [] as string[] }) {
  if (!rows.length) return <p>Пусто</p>
  const keys = Object.keys(rows[0]).filter(k => !hiddenFields.includes(k))
  return <table><thead><tr>{keys.map(k => <th key={k}>{fieldLabels[k] ?? translateValue(k)}</th>)}</tr></thead><tbody>{rows.map((r, i) => <tr key={i}>{keys.map(k => <td key={k}>{translateValue(r[k])}</td>)}</tr>)}</tbody></table>
}

createRoot(document.getElementById('root')!).render(<React.StrictMode><App /></React.StrictMode>)
