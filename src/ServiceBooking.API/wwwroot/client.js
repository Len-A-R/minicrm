const clientApi = {
  me: "/api/v1/client/me",
  bookings: "/api/v1/client/bookings",
  notifications: "/api/v1/client/notifications"
};

const clientMenuItems = [
  { route: "booking", label: "Запись", icon: "calendar" },
  { route: "history", label: "История", icon: "history" },
  { route: "notifications", label: "Уведомления", icon: "bell" },
  { route: "profile", label: "Профиль", icon: "user" }
];

const clientState = {
  route: "booking",
  token: getClientAuthValue("serviceBookingAccessToken") || "",
  role: getClientAuthValue("serviceBookingUserRole") || "",
  profile: null,
  refreshTimerId: 0
};

const clientEls = {
  sidebar: document.querySelector("#client-sidebar"),
  nav: document.querySelector("#client-nav"),
  logout: document.querySelector("#client-logout-button"),
  sections: [...document.querySelectorAll("[data-client-section]")],
  historyList: document.querySelector("#client-history-list"),
  notificationsList: document.querySelector("#client-notifications-list"),
  profileForm: document.querySelector("#client-profile-form"),
  profileFullName: document.querySelector("#client-profile-full-name"),
  profilePhone: document.querySelector("#client-profile-phone"),
  profileFullNameError: document.querySelector("#client-profile-full-name-error"),
  profilePhoneError: document.querySelector("#client-profile-phone-error"),
  profileSave: document.querySelector("#client-profile-save-button"),
  profileResult: document.querySelector("#client-profile-result"),
  toast: document.querySelector("#client-toast")
};

document.addEventListener("DOMContentLoaded", initClientPortal);

function initClientPortal() {
  if (!clientState.token || clientState.role !== "Client") {
    redirectToClientLogin();
    return;
  }

  clientState.route = getClientRouteFromLocation();
  buildClientMenu();
  clientEls.logout.addEventListener("click", logoutClient);
  clientEls.profileForm.addEventListener("submit", submitClientProfile);
  clientEls.profileFullName.addEventListener("input", validateClientProfile);
  clientEls.profilePhone.addEventListener("input", () => {
    clientEls.profilePhone.value = clientEls.profilePhone.value.replace(/[^\d+ ().-]/g, "");
    validateClientProfile();
  });
  window.addEventListener("popstate", () => {
    clientState.route = getClientRouteFromLocation();
    loadClientSection(false);
  });
  loadClientSection(false);
  startClientAutoRefresh();
}

function buildClientMenu() {
  clientEls.nav.innerHTML = clientMenuItems
    .map((item) => `
      <button type="button" class="dashboard-nav-item client-nav-item" data-client-route="${item.route}" aria-label="${escapeClientHtml(item.label)}">
        ${clientNavIcon(item.icon)}
        <span>${escapeClientHtml(item.label)}</span>
      </button>`)
    .join("");
  clientEls.nav.querySelectorAll("[data-client-route]").forEach((button) => {
    button.addEventListener("click", () => navigateClient(button.dataset.clientRoute));
  });
  clientEls.sidebar.hidden = false;
  document.body.classList.remove("no-sidebar");
  updateClientChrome();
}

function getClientRouteFromLocation() {
  const route = new URLSearchParams(location.search).get("section");
  return clientMenuItems.some((item) => item.route === route) ? route : "booking";
}

function navigateClient(route) {
  clientState.route = route;
  history.pushState({}, "", `/client.html?section=${route}`);
  loadClientSection(false);
}

async function loadClientSection(force) {
  updateClientChrome();
  clientEls.sections.forEach((section) => {
    section.hidden = section.dataset.clientSection !== clientState.route;
  });

  try {
    if (clientState.route === "history") {
      await renderClientHistory(force);
    } else if (clientState.route === "notifications") {
      await renderClientNotifications(force);
    } else if (clientState.route === "profile") {
      await renderClientProfile(force);
    } else if (force) {
      await refreshClientProfile();
    }
  } catch (error) {
    if (error.status === 401 || error.status === 403) {
      logoutClient();
      return;
    }
    showClientToast(error.message, true);
  }
}

async function refreshClientProfile() {
  clientState.profile = await clientRequest(clientApi.me);
  syncBookingClientFields(clientState.profile);
}

function startClientAutoRefresh() {
  window.clearInterval(clientState.refreshTimerId);
  clientState.refreshTimerId = window.setInterval(() => {
    if (clientState.route === "profile") {
      return;
    }
    loadClientSection(true);
  }, 30000);
}

function updateClientChrome() {
  clientEls.nav.querySelectorAll("[data-client-route]").forEach((button) => {
    button.classList.toggle("active", button.dataset.clientRoute === clientState.route);
  });
}

async function renderClientProfile(force) {
  if (!clientState.profile || force) {
    clientState.profile = await clientRequest(clientApi.me);
  }

  clientEls.profileFullName.value = clientState.profile.fullName || "";
  clientEls.profilePhone.value = clientState.profile.phone || "";
  syncBookingClientFields(clientState.profile);
  showClientProfileResult("", false);
  validateClientProfile();
}

async function submitClientProfile(event) {
  event.preventDefault();
  if (!validateClientProfile() || clientEls.profileSave.disabled) {
    return;
  }

  clientEls.profileSave.disabled = true;
  clientEls.profileSave.textContent = "Сохранение...";
  showClientProfileResult("", false);

  try {
    const profile = await clientRequest(clientApi.me, {
      method: "PUT",
      body: {
        fullName: clientEls.profileFullName.value.trim(),
        phone: clientEls.profilePhone.value.trim()
      }
    });
    clientState.profile = profile;
    syncBookingClientFields(profile);
    showClientProfileResult("Профиль обновлён.", false);
  } catch (error) {
    showClientProfileResult(error.message, true);
  } finally {
    clientEls.profileSave.textContent = "Сохранить";
    validateClientProfile();
  }
}

function validateClientProfile() {
  const fullName = clientEls.profileFullName.value.trim();
  const phone = clientEls.profilePhone.value.trim();
  const fullNameValid = /^[\p{L} ]{2,100}$/u.test(fullName);
  const phoneValid = /^\+?[0-9][0-9\s().-]{6,30}$/.test(phone);

  clientEls.profileFullNameError.textContent = fullName && !fullNameValid ? "Только буквы и пробелы, минимум 2 символа." : "";
  clientEls.profilePhoneError.textContent = phone && !phoneValid ? "Проверьте формат телефона." : "";
  clientEls.profileSave.disabled = !(fullNameValid && phoneValid);
  return fullNameValid && phoneValid;
}

function syncBookingClientFields(profile) {
  window.serviceBookingBookingApp?.setClientProfile?.(profile);
}

async function renderClientHistory() {
  clientEls.historyList.innerHTML = `<div class="empty-state">Загрузка...</div>`;
  const bookings = await clientRequest(clientApi.bookings);
  if (bookings.length === 0) {
    clientEls.historyList.innerHTML = `<div class="empty-state">История записей пока пустая.</div>`;
    return;
  }

  clientEls.historyList.innerHTML = bookings.map(renderHistoryCard).join("");
  clientEls.historyList.querySelectorAll("[data-repeat-booking]").forEach((button) => {
    button.addEventListener("click", () => {
      const booking = bookings.find((item) => item.id === button.dataset.repeatBooking);
      if (!booking || !window.serviceBookingBookingApp?.repeatBooking) {
        showClientToast("Не удалось подготовить повторную запись.", true);
        return;
      }
      navigateClient("booking");
      window.serviceBookingBookingApp.repeatBooking(booking)
        .catch(() => showClientToast("Не удалось подготовить повторную запись.", true));
    });
  });
}

function renderHistoryCard(booking) {
  const services = booking.services?.map((service) => service.serviceName).join(", ") || "Без услуги";
  const requested = `${formatClientDate(booking.requestedDate)} в ${formatClientTime(booking.requestedTime)}`;
  const scheduled = booking.confirmedDate
    ? `${formatClientDate(booking.confirmedDate)} в ${formatClientTime(booking.confirmedTime || booking.requestedTime)}`
    : requested;
  const reply = booking.specialistReply
    ? `<p class="client-card-reply">${escapeClientHtml(booking.specialistReply)}</p>`
    : "";

  return `
    <article class="client-history-card">
      <div class="client-card-main">
        <div>
          <h3>${escapeClientHtml(booking.specialistName)}</h3>
          <p>${escapeClientHtml(services)}</p>
        </div>
        <span class="status-pill">${escapeClientHtml(translateStatus(booking.status))}</span>
      </div>
      <dl class="client-card-meta">
        <div><dt>Когда</dt><dd>${escapeClientHtml(scheduled)}</dd></div>
        <div><dt>Цена</dt><dd>${formatClientMoney(booking.totalPrice)}</dd></div>
      </dl>
      ${reply}
      <button type="button" class="secondary-button" data-repeat-booking="${escapeClientHtml(booking.id)}">Записаться повторно</button>
    </article>`;
}

async function renderClientNotifications() {
  clientEls.notificationsList.innerHTML = `<div class="empty-state">Загрузка...</div>`;
  const notifications = await clientRequest(clientApi.notifications);
  if (notifications.length === 0) {
    clientEls.notificationsList.innerHTML = `<div class="empty-state">Новых ответов от специалистов нет.</div>`;
    return;
  }

  clientEls.notificationsList.innerHTML = notifications
    .map((notification) => `
      <article class="client-notification-card">
        <div class="client-card-main">
          <h3>${escapeClientHtml(notification.specialistName)}</h3>
          <time>${escapeClientHtml(formatClientDateTime(notification.repliedAt))}</time>
        </div>
        <p>${escapeClientHtml(notification.reply)}</p>
      </article>`)
    .join("");
}

function logoutClient() {
  clearClientAuth(localStorage);
  clearClientAuth(sessionStorage);
  redirectToClientLogin();
}

function redirectToClientLogin() {
  location.href = "/login";
}

async function clientRequest(url, options = {}) {
  const response = await fetch(url, {
    method: options.method || "GET",
    headers: {
      Authorization: `Bearer ${clientState.token}`,
      ...(options.body ? { "Content-Type": "application/json" } : {})
    },
    body: options.body ? JSON.stringify(options.body) : undefined
  });
  const data = await response.json().catch(() => null);
  if (!response.ok) {
    const error = new Error(data?.message || `HTTP ${response.status}`);
    error.status = response.status;
    throw error;
  }
  return data;
}

function showClientProfileResult(message, isError) {
  clientEls.profileResult.textContent = message;
  clientEls.profileResult.classList.toggle("visible", Boolean(message));
  clientEls.profileResult.classList.toggle("error", isError);
}

function getClientAuthValue(key) {
  return localStorage.getItem(key) || sessionStorage.getItem(key);
}

function clearClientAuth(storage) {
  storage.removeItem("serviceBookingAccessToken");
  storage.removeItem("serviceBookingRefreshToken");
  storage.removeItem("serviceBookingAdminAccessToken");
  storage.removeItem("serviceBookingUserRole");
}

function showClientToast(message, isError = false) {
  clientEls.toast.textContent = message;
  clientEls.toast.classList.toggle("visible", Boolean(message));
  clientEls.toast.classList.toggle("error", isError);
  if (message) {
    window.setTimeout(() => clientEls.toast.classList.remove("visible"), 3200);
  }
}

function translateStatus(status) {
  const map = {
    New: "Новая",
    Confirmed: "Подтверждена",
    Rejected: "Отклонена",
    Completed: "Завершена"
  };
  return map[status] || status || "Новая";
}

function formatClientMoney(value) {
  return new Intl.NumberFormat("ru-RU", {
    style: "currency",
    currency: "RUB",
    maximumFractionDigits: 0
  }).format(value || 0);
}

function formatClientDate(value) {
  if (!value) return "";
  return new Intl.DateTimeFormat("ru-RU").format(new Date(`${value}T00:00:00`));
}

function formatClientTime(value) {
  return String(value || "").slice(0, 5);
}

function formatClientDateTime(value) {
  return new Intl.DateTimeFormat("ru-RU", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function escapeClientHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function clientNavIcon(name) {
  const icons = {
    calendar: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 3v3M17 3v3M4 9h16M6 5h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2Z"/></svg>`,
    history: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 12a9 9 0 1 0 3-6.7M3 4v6h6M12 7v5l3 2"/></svg>`,
    bell: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4"/></svg>`,
    user: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 21a8 8 0 0 0-16 0M12 13a5 5 0 1 0 0-10 5 5 0 0 0 0 10Z"/></svg>`
  };
  return icons[name] || "";
}
