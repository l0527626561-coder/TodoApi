import React, { useEffect, useState } from 'react';
import service from './service.js';
import './App.css';

function App() {
  const [page, setPage] = useState('login');
  const [user, setUser] = useState(null);
  const [newTodo, setNewTodo] = useState("");
  const [todos, setTodos] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // Auth form states
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  async function getTodos() {
    try {
      const todos = await service.getTasks();
      setTodos(todos);
    } catch (err) {
      setError("Failed to load todos");
    }
  }

  async function createTodo(e) {
    e.preventDefault();
    if (!newTodo.trim()) return;
    try {
      setLoading(true);
      await service.addTask(newTodo);
      setNewTodo("");
      await getTodos();
    } catch (err) {
      setError("Failed to create todo");
    } finally {
      setLoading(false);
    }
  }

  async function updateCompleted(todo, isComplete) {
    try {
      await service.setCompleted(todo.id, todo.name, isComplete);
      await getTodos();
    } catch (err) {
      setError("Failed to update todo");
    }
  }

  async function deleteTodo(id) {
    try {
      await service.deleteTask(id);
      await getTodos();
    } catch (err) {
      setError("Failed to delete todo");
    }
  }

  async function handleLogin(e) {
    e.preventDefault();
    try {
      setLoading(true);
      setError("");
      const result = await service.login(username, password);
      setUser(result);
      setPage('todos');
      setUsername("");
      setPassword("");
      await getTodos();
    } catch (err) {
      setError("Login failed. Check your credentials.");
    } finally {
      setLoading(false);
    }
  }

  async function handleRegister(e) {
    e.preventDefault();
    try {
      setLoading(true);
      setError("");
      const result = await service.register(username, password);
      setUser(result);
      setPage('todos');
      setUsername("");
      setPassword("");
      await getTodos();
    } catch (err) {
      setError("Registration failed. Username may already exist.");
    } finally {
      setLoading(false);
    }
  }

  function handleLogout() {
    service.logout();
    setUser(null);
    setPage('login');
    setTodos([]);
    setNewTodo("");
    setUsername("");
    setPassword("");
  }

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (token) {
      setPage('todos');
      getTodos();
    }
  }, []);

  if (page === 'login') {
    return (
      <div className="auth-container">
        <div className="auth-box">
          <h1>Todo App</h1>
          <form onSubmit={handleLogin}>
            <input
              type="text"
              placeholder="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
            />
            <input
              type="password"
              placeholder="Password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
            <button type="submit" disabled={loading}>
              {loading ? 'Logging in...' : 'Login'}
            </button>
          </form>
          {error && <p className="error">{error}</p>}
          <p>
            Don't have an account?{' '}
            <button type="button" onClick={() => setPage('register')} className="link-btn">
              Register
            </button>
          </p>
        </div>
      </div>
    );
  }

  if (page === 'register') {
    return (
      <div className="auth-container">
        <div className="auth-box">
          <h1>Create Account</h1>
          <form onSubmit={handleRegister}>
            <input
              type="text"
              placeholder="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
            />
            <input
              type="password"
              placeholder="Password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
            <button type="submit" disabled={loading}>
              {loading ? 'Registering...' : 'Register'}
            </button>
          </form>
          {error && <p className="error">{error}</p>}
          <p>
            Already have an account?{' '}
            <button type="button" onClick={() => setPage('login')} className="link-btn">
              Login
            </button>
          </p>
        </div>
      </div>
    );
  }

  return (
    <section className="todoapp">
      <header className="header">
        <div className="header-top">
          <h1>todos</h1>
          <button className="logout-btn" onClick={handleLogout}>
            Logout ({user?.username})
          </button>
        </div>
        <form onSubmit={createTodo}>
          <input
            className="new-todo"
            placeholder="Well, let's take on the day"
            value={newTodo}
            onChange={(e) => setNewTodo(e.target.value)}
            disabled={loading}
          />
        </form>
      </header>
      {error && <div className="error-message">{error}</div>}
      <section className="main" style={{ display: "block" }}>
        <ul className="todo-list">
          {todos.map(todo => {
            return (
              <li className={todo.isComplete ? "completed" : ""} key={todo.id}>
                <div className="view">
                  <input
                    className="toggle"
                    type="checkbox"
                    checked={todo.isComplete}
                    onChange={(e) => updateCompleted(todo, e.target.checked)}
                  />
                  <label>{todo.name}</label>
                  <button className="destroy" onClick={() => deleteTodo(todo.id)}></button>
                </div>
              </li>
            );
          })}
        </ul>
      </section>
    </section>
  );
}

export default App;