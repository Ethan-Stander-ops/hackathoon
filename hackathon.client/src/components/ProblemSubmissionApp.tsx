import React, { useState, FormEvent, ChangeEvent } from "react";
import styles from "./ProblemSubmissionApp.module.scss";

interface ProblemSubmission {
  title: string;
  description: string;
  email: string;
}

const ProblemSubmissionApp: React.FC = () => {
  // State for the current problem form
  const [problem, setProblem] = useState<ProblemSubmission>({
    title: "",
    description: "",
    email: "",
  });

  // State for storing submitted problems in memory
  const [problems, setProblems] = useState<ProblemSubmission[]>([]);

  // State for displaying the submission response
  const [submitResponse, setSubmitResponse] = useState<string>("");

  // State for displaying trends from the API
  const [trends, setTrends] = useState<string>("");

  // Submit a new problem
  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    try {
      const response = await fetch("https://localhost:7208/api/problems/submit", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(problem),
      });
      const result = await response.json();
      console.log(result);
      if (response.ok) {
        setSubmitResponse(result.message);
        // Store the problem in local memory for later trend analysis.
        setProblems((prev) => [...prev, problem]);
        // Clear form fields.
        setProblem({ title: "", description: "", email: "" });
      } else {
        setSubmitResponse("Error: " + result.message);
      }
    } catch (error) {
      console.error(error);
      setSubmitResponse("Error submitting problem.");
    }
  };

  // Fetch common trends from the API
  const fetchTrends = async () => {
    try {
      const response = await fetch("https://localhost:7208/api/problems/trends", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(problems),
      });
      const result = await response.json();
      setTrends(result.trends);
    } catch (error) {
      console.error(error);
      setTrends("Error fetching trends.");
    }
  };

  return (
    <div className={styles.container}>
      <h1 className={styles.title}>Problem Submission</h1>

      {/* Submission Form */}
      <form onSubmit={handleSubmit} className={styles.form}>
        <div className={styles.formGroup}>
          <label className={styles.label}>Title:</label>
          <input
            type="text"
            className={styles.input}
            value={problem.title}
            onChange={(e: ChangeEvent<HTMLInputElement>) =>
              setProblem({ ...problem, title: e.target.value })
            }
            required
          />
        </div>

        <div className={styles.formGroup}>
          <label className={styles.label}>Description:</label>
          <textarea
            className={styles.textarea}
            value={problem.description}
            onChange={(e: ChangeEvent<HTMLTextAreaElement>) =>
              setProblem({ ...problem, description: e.target.value })
            }
            required
          ></textarea>
        </div>

        <div className={styles.formGroup}>
          <label className={styles.label}>Email:</label>
          <input
            type="email"
            className={styles.input}
            value={problem.email}
            onChange={(e: ChangeEvent<HTMLInputElement>) =>
              setProblem({ ...problem, email: e.target.value })
            }
            required
          />
        </div>

        <button type="submit" className={styles.button}>
          Submit Problem
        </button>
      </form>

      {/* Submission Response */}
      {submitResponse && (
        <div>
          <strong>Submission Response:</strong> {submitResponse}
        </div>
      )}

      <div className={styles.problemsSection}>
        <h2>Submitted Problems (In-Memory)</h2>
        {problems.length === 0 ? (
          <p>No problems submitted yet.</p>
        ) : (
          <ul className={styles.problemsList}>
            {problems.map((p, index) => (
              <li key={index} className={styles.problemItem}>
                <strong>{p.title}</strong> – {p.description}
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className={styles.trendsSection}>
        <h2>Common Trends</h2>
        <button onClick={fetchTrends} className={styles.trendsButton}>
          Get Trends
        </button>
        {trends && (
          <div className={styles.trendsOutput}>
            <strong>Trends:</strong> {trends}
          </div>
        )}
      </div>
    </div>
  );
};

export default ProblemSubmissionApp;
