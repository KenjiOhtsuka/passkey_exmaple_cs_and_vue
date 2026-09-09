<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { authApi } from '../api'

const router = useRouter()
const email = ref('')
const password = ref('')
const error = ref('')
const busy = ref(false)

async function submit() {
  error.value = ''
  busy.value = true
  try {
    await authApi.register(email.value, password.value)
    await authApi.login(email.value, password.value)
    router.push('/dashboard')
  } catch (err) {
    error.value = err.message || 'Registration failed.'
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <h1>Register</h1>
  <form @submit.prevent="submit">
    <label for="email">Email</label>
    <input id="email" v-model="email" type="email" autocomplete="username" required />

    <label for="password">Password</label>
    <input id="password" v-model="password" type="password" autocomplete="new-password" minlength="6" required />

    <button type="submit" :disabled="busy">Create account</button>
  </form>

  <p class="error">{{ error }}</p>
</template>