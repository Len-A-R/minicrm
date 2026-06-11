const registerApi = {
  register: "/api/v1/auth/register"
};

const registerEls = {
  form: document.querySelector("#register-form"),
  fullName: document.querySelector("#register-full-name"),
  email: document.querySelector("#register-email"),
  phone: document.querySelector("#register-phone"),
  password: document.querySelector("#register-password"),
  confirmPassword: document.querySelector("#register-confirm-password"),
  submit: document.querySelector("#register-submit"),
  result: document.querySelector("#register-result"),
  errors: {
    fullName: document.querySelector("#register-full-name-error"),
    email: document.querySelector("#register-email-error"),
    phone: document.querySelector("#register-phone-error"),
    password: document.querySelector("#register-password-error"),
    confirmPassword: document.querySelector("#register-confirm-password-error")
  }
};

document.addEventListener("DOMContentLoaded", initRegister);

function initRegister() {
  registerEls.form.addEventListener("submit", submitRegistration);
  [
    registerEls.fullName,
    registerEls.email,
    registerEls.phone,
    registerEls.password,
    registerEls.confirmPassword
  ].forEach((input) => input.addEventListener("input", validateRegistration));

  registerEls.phone.addEventListener("input", () => {
    registerEls.phone.value = registerEls.phone.value.replace(/[^\d+ ().-]/g, "");
    validateRegistration();
  });

  validateRegistration();
}

async function submitRegistration(event) {
  event.preventDefault();
  if (!validateRegistration() || registerEls.submit.disabled) {
    return;
  }

  const payload = {
    fullName: registerEls.fullName.value.trim(),
    email: registerEls.email.value.trim(),
    phone: registerEls.phone.value.trim(),
    password: registerEls.password.value,
    confirmPassword: registerEls.confirmPassword.value
  };

  registerEls.submit.disabled = true;
  registerEls.submit.textContent = "Регистрация...";
  showRegisterResult("", false);

  try {
    const auth = await postJson(registerApi.register, payload);
    localStorage.setItem("serviceBookingAccessToken", auth.accessToken);
    localStorage.setItem("serviceBookingRefreshToken", auth.refreshToken);
    showRegisterResult("Аккаунт создан.", false);
    window.location.href = "/dashboard.html";
  } catch (error) {
    showRegisterResult(error.message, true);
  } finally {
    registerEls.submit.textContent = "Зарегистрироваться";
    validateRegistration();
  }
}

function validateRegistration() {
  const fullName = registerEls.fullName.value.trim();
  const email = registerEls.email.value.trim();
  const phone = registerEls.phone.value.trim();
  const password = registerEls.password.value;
  const confirmPassword = registerEls.confirmPassword.value;

  const fullNameValid = /^[\p{L} ]{2,100}$/u.test(fullName);
  const emailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  const phoneValid = /^\+?[0-9][0-9\s().-]{6,30}$/.test(phone);
  const passwordValid = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/.test(password);
  const confirmPasswordValid = confirmPassword.length > 0 && password === confirmPassword;

  registerEls.errors.fullName.textContent = fullName && !fullNameValid ? "Только буквы и пробелы, минимум 2 символа." : "";
  registerEls.errors.email.textContent = email && !emailValid ? "Проверьте email." : "";
  registerEls.errors.phone.textContent = phone && !phoneValid ? "Проверьте формат телефона." : "";
  registerEls.errors.password.textContent = password && !passwordValid ? "Минимум 8 символов, заглавная, строчная буква и цифра." : "";
  registerEls.errors.confirmPassword.textContent = confirmPassword && !confirmPasswordValid ? "Пароли не совпадают." : "";

  const valid = fullNameValid && emailValid && phoneValid && passwordValid && confirmPasswordValid;
  registerEls.submit.disabled = !valid;
  return valid;
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(data?.message || `HTTP ${response.status}`);
  }

  return data;
}

function showRegisterResult(message, isError) {
  registerEls.result.textContent = message;
  registerEls.result.classList.toggle("visible", Boolean(message));
  registerEls.result.classList.toggle("error", isError);
}
