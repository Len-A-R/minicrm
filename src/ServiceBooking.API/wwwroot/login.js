const loginApi = {
  login: "/api/v1/auth/login"
};

document.addEventListener("DOMContentLoaded", initLoginPage);

function initLoginPage() {
  document.querySelector("#login-form").addEventListener("submit", submitLogin);
}

async function submitLogin(event) {
  event.preventDefault();
  const result = document.querySelector("#login-result");
  result.className = "result-banner auth-result";
  result.textContent = "";

  try {
    const auth = await requestLogin(loginApi.login, {
      email: document.querySelector("#login-email").value.trim(),
      password: document.querySelector("#login-password").value
    });
    persistAuth(auth, document.querySelector("#login-remember").checked);
    redirectByRole(auth.role);
  } catch (error) {
    result.textContent = error.message;
    result.classList.add("visible", "error");
  }
}

function persistAuth(auth, remember) {
  clearAuthStorage(localStorage);
  clearAuthStorage(sessionStorage);
  const storage = remember ? localStorage : sessionStorage;
  storage.setItem("serviceBookingUserRole", auth.role);
  storage.setItem("serviceBookingAccessToken", auth.accessToken);

  if (auth.refreshToken) {
    storage.setItem("serviceBookingRefreshToken", auth.refreshToken);
  }

  if (auth.role === "Admin") {
    storage.setItem("serviceBookingAdminAccessToken", auth.accessToken);
  }
}

function clearAuthStorage(storage) {
  storage.removeItem("serviceBookingAccessToken");
  storage.removeItem("serviceBookingRefreshToken");
  storage.removeItem("serviceBookingAdminAccessToken");
  storage.removeItem("serviceBookingUserRole");
}

function redirectByRole(role) {
  if (role === "Admin") {
    location.href = "/admin.html";
    return;
  }

  if (role === "Client") {
    location.href = "/client.html";
    return;
  }

  location.href = "/dashboard.html";
}

async function requestLogin(url, body) {
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    throw new Error(data?.message || `HTTP ${response.status}`);
  }
  return data;
}
