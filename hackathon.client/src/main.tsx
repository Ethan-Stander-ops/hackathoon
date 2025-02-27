import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import ProblemSubmissionApp from './components/ProblemSubmissionApp'


createRoot(document.getElementById('root')!).render(
  <StrictMode>
   <ProblemSubmissionApp />
  </StrictMode>,
)
