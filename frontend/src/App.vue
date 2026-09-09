<script setup>
</script>

<template>
  <header>
    <nav>
      <RouterLink to="/">Sign in</RouterLink>
      <RouterLink to="/register">Register</RouterLink>
    </nav>
  </header>
  <main>
    <RouterView />
  </main>
</template>

<style>
:root {
  font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
  color: #1f2328;
}

body {
  margin: 0;
  background: #f6f8fa;
}

nav {
  display: flex;
  gap: 1rem;
  padding: 0.75rem 1.5rem;
  background: #24292f;
}

nav a {
  color: #fff;
  text-decoration: none;
}

nav a:hover {
  text-decoration: underline;
}

main {
  max-width: 480px;
  margin: 3rem auto;
  padding: 2rem;
  background: #fff;
  border: 1px solid #d0d7de;
  border-radius: 8px;
}

h1 {
  margin-top: 0;
  font-size: 1.5rem;
}

label {
  display: block;
  font-weight: 600;
  margin: 1rem 0 0.25rem;
}

input[type="email"],
input[type="password"],
input[type="text"] {
  width: 100%;
  box-sizing: border-box;
  padding: 0.5rem 0.75rem;
  border: 1px solid #d0d7de;
  border-radius: 6px;
}

button {
  margin-top: 1.25rem;
  padding: 0.6rem 1rem;
  border: none;
  border-radius: 6px;
  background: #0969da;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

button.secondary {
  background: #eaeef2;
  color: #1f2328;
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  color: #cf222e;
  margin-top: 0.5rem;
  white-space: pre-line;
}

ul {
  padding-left: 1.25rem;
}

li {
  margin: 0.25rem 0;
}
</style>