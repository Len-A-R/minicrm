const dashboardApi = {
  login: "/api/v1/auth/login",
  bookings: "/api/v1/specialist/bookings",
  booking: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}`,
  confirm: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}/confirm`,
  reject: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}/reject`,
  complete: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}/complete`,
  reply: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}/reply`,
  clients: "/api/v1/specialist/clients",
  clientStatus: (id) => `/api/v1/specialist/clients/${encodeURIComponent(id)}/status`,
  clientTag: (id) => `/api/v1/specialist/clients/${encodeURIComponent(id)}/tag`
};

const dashboardState = {
  route: "bookings",
  cache: new Map(),
  bookingFilters: { status: "", date: "", search: "" },
  token: localStorage.getItem("serviceBookingAccessToken") || ""
};

const dashboardEls = {
  nav: [...document.querySelectorAll(".dashboard-nav-item")],
  content: document.querySelector("#dashboard-content"),
  title: document.querySelector("#dashboard-title"),
  label: document.querySelector("#dashboard-section-label"),
  refresh: document.querySelector("#refresh-button"),
  loginPanel: document.querySelector("#login-panel"),
  loginForm: document.querySelector("#login-form"),
  logout: document.querySelector("#logout-button"),
  toast: document.querySelector("#dashboard-toast"),
  dialog: document.querySelector("#action-dialog"),
  dialogTitle: document.querySelector("#dialog-title"),
  dialogBody: document.querySelector("#dialog-body"),
  dialogClose: document.querySelector("#dialog-close"),
  dialogCancel: document.querySelector("#dialog-cancel"),
  actionForm: document.querySelector("#action-form"),
  dialogSubmit: document.querySelector("#dialog-submit")
};

document.addEventListener("DOMContentLoaded", initDashboard);

function initDashboard() {
  dashboardEls.nav.forEach((button) => {
    button.addEventListener("click", () => navigate(button.dataset.route));
  });
  dashboardEls.refresh.addEventListener("click", () => loadRoute(true));
  dashboardEls.logout.addEventListener("click", logout);
  dashboardEls.loginForm.addEventListener("submit", login);
  dashboardEls.dialogClose.addEventListener("click", closeDialog);
  dashboardEls.dialogCancel.addEventListener("click", closeDialog);
  window.addEventListener("popstate", () => {
    dashboardState.route = getRouteFromLocation();
    loadRoute(false);
  });

  dashboardState.route = getRouteFromLocation();
  loadRoute(false);
}

function getRouteFromLocation() {
  const route = new URLSearchParams(location.search).get("section");
  return route === "clients" ? "clients" : "bookings";
}

function navigate(route) {
  dashboardState.route = route;
  history.pushState({}, "", `/dashboard.html?section=${route}`);
  loadRoute(false);
}

async function login(event) {
  event.preventDefault();
  const payload = {
    email: document.querySelector("#login-email").value.trim(),
    password: document.querySelector("#login-password").value
  };

  try {
    const auth = await requestJson(dashboardApi.login, { method: "POST", body: payload, auth: false });
    dashboardState.token = auth.accessToken;
    localStorage.setItem("serviceBookingAccessToken", auth.accessToken);
    dashboardEls.loginPanel.hidden = true;
    dashboardState.cache.clear();
    await loadRoute(true);
  } catch (error) {
    showDashboardToast(error.message, true);
  }
}

function logout() {
  dashboardState.token = "";
  dashboardState.cache.clear();
  localStorage.removeItem("serviceBookingAccessToken");
  loadRoute(true);
}

async function loadRoute(force) {
  updateChrome();
  if (!dashboardState.token) {
    dashboardEls.loginPanel.hidden = false;
    dashboardEls.content.innerHTML = "";
    return;
  }

  dashboardEls.loginPanel.hidden = true;
  dashboardEls.content.classList.add("is-transitioning");
  setTimeout(async () => {
    try {
      if (dashboardState.route === "clients") {
        await renderClients(force);
      } else {
        await renderBookings(force);
      }
      dashboardEls.content.classList.remove("is-transitioning");
    } catch (error) {
      dashboardEls.content.classList.remove("is-transitioning");
      if (error.status === 401) {
        logout();
      } else {
        dashboardEls.content.innerHTML = `<div class="empty-state">${escapeDashboardHtml(error.message)}</div>`;
      }
    }
  }, 180);
}

function updateChrome() {
  const isClients = dashboardState.route === "clients";
  dashboardEls.title.textContent = isClients ? "Клиентская база" : "Управление заявками";
  dashboardEls.label.textContent = isClients ? "Клиенты" : "Заявки";
  dashboardEls.nav.forEach((button) => button.classList.toggle("active", button.dataset.route === dashboardState.route));
}

async function renderBookings(force) {
  dashboardEls.content.innerHTML = `<div class="empty-state">Загрузка...</div>`;
  const query = new URLSearchParams();
  if (dashboardState.bookingFilters.status) query.set("status", dashboardState.bookingFilters.status);
  if (dashboardState.bookingFilters.date) query.set("date", dashboardState.bookingFilters.date);
  if (dashboardState.bookingFilters.search) query.set("search", dashboardState.bookingFilters.search);
  query.set("page", "1");
  query.set("pageSize", "50");
  const cacheKey = `bookings:${query.toString()}`;
  const data = await getCached(cacheKey, `${dashboardApi.bookings}?${query}`, force);

  dashboardEls.content.innerHTML = `
    <div class="table-toolbar">
      <select id="booking-status-filter">
        <option value="">Все статусы</option>
        <option value="New">Новые</option>
        <option value="Confirmed">Подтвержденные</option>
        <option value="Rejected">Отклоненные</option>
        <option value="Completed">Выполненные</option>
      </select>
      <input id="booking-date-filter" type="date">
      <input id="booking-search-filter" type="search" placeholder="Имя или телефон">
    </div>
    <div class="data-table-wrap">
      <table class="data-table">
        <thead>
          <tr>
            <th>ID</th><th>Создана</th><th>Клиент</th><th>Запрошено</th><th>Услуги</th><th>Сумма</th><th>Статус</th><th>Действия</th>
          </tr>
        </thead>
        <tbody>${data.items.map(renderBookingRow).join("") || `<tr><td colspan="8">Заявки не найдены.</td></tr>`}</tbody>
      </table>
    </div>`;

  document.querySelector("#booking-status-filter").value = dashboardState.bookingFilters.status;
  document.querySelector("#booking-date-filter").value = dashboardState.bookingFilters.date;
  document.querySelector("#booking-search-filter").value = dashboardState.bookingFilters.search;
  document.querySelector("#booking-status-filter").addEventListener("change", updateBookingFilters);
  document.querySelector("#booking-date-filter").addEventListener("change", updateBookingFilters);
  document.querySelector("#booking-search-filter").addEventListener("input", debounce(updateBookingFilters, 250));
  dashboardEls.content.querySelectorAll("[data-booking-action]").forEach((button) => {
    button.addEventListener("click", () => openBookingAction(button.dataset.bookingAction, button.dataset.bookingId));
  });
}

function renderBookingRow(booking) {
  const services = booking.services.map((service) => service.serviceName).join(", ") || "Без услуги";
  return `
    <tr>
      <td><span class="mono">${escapeDashboardHtml(booking.id.slice(0, 8))}</span></td>
      <td>${formatDateTime(booking.createdAt)}</td>
      <td>${escapeDashboardHtml(booking.clientName)}<span class="muted-cell">${escapeDashboardHtml(booking.clientPhone)}</span></td>
      <td>${escapeDashboardHtml(booking.requestedDate)} ${escapeDashboardHtml(booking.requestedTime.slice(0, 5))}</td>
      <td>${escapeDashboardHtml(services)}</td>
      <td>${formatDashboardMoney(booking.totalPrice)}</td>
      <td><span class="status-badge status-${String(booking.status).toLowerCase()}">${statusText(booking.status)}</span></td>
      <td>
        <div class="row-actions">
          <button type="button" class="table-action" data-booking-action="reply" data-booking-id="${booking.id}">Ответ</button>
          <button type="button" class="table-action" data-booking-action="confirm" data-booking-id="${booking.id}">Подтвердить</button>
          <button type="button" class="table-action" data-booking-action="reject" data-booking-id="${booking.id}">Отклонить</button>
          <button type="button" class="table-action" data-booking-action="complete" data-booking-id="${booking.id}">Выполнена</button>
        </div>
      </td>
    </tr>`;
}

function updateBookingFilters() {
  dashboardState.bookingFilters = {
    status: document.querySelector("#booking-status-filter").value,
    date: document.querySelector("#booking-date-filter").value,
    search: document.querySelector("#booking-search-filter").value.trim()
  };
  renderBookings(true);
}

async function renderClients(force) {
  dashboardEls.content.innerHTML = `<div class="empty-state">Загрузка...</div>`;
  const clients = await getCached("clients", dashboardApi.clients, force);
  dashboardEls.content.innerHTML = `
    <div class="data-table-wrap">
      <table class="data-table">
        <thead>
          <tr><th>Клиент</th><th>Заявок</th><th>Последняя заявка</th><th>Статус</th><th>Метка</th><th>Действия</th></tr>
        </thead>
        <tbody>${clients.map(renderClientRow).join("") || `<tr><td colspan="6">Клиенты не найдены.</td></tr>`}</tbody>
      </table>
    </div>`;

  dashboardEls.content.querySelectorAll("[data-client-status]").forEach((select) => {
    select.addEventListener("change", () => updateClientStatus(select.dataset.clientStatus, Number(select.value)));
  });
  dashboardEls.content.querySelectorAll("[data-client-tag]").forEach((input) => {
    input.addEventListener("change", () => updateClientTag(input.dataset.clientTag, input.value));
  });
}

function renderClientRow(client) {
  return `
    <tr>
      <td>${escapeDashboardHtml(client.fullName)}<span class="muted-cell">${escapeDashboardHtml(client.phone)}</span></td>
      <td>${client.bookingCount}</td>
      <td>${client.lastBookingAt ? formatDateTime(client.lastBookingAt) : "-"}</td>
      <td>
        <select data-client-status="${client.id}">
          <option value="1"${client.status === 1 ? " selected" : ""}>Обычный</option>
          <option value="2"${client.status === 2 ? " selected" : ""}>VIP</option>
          <option value="3"${client.status === 3 ? " selected" : ""}>Забанен</option>
        </select>
      </td>
      <td><input data-client-tag="${client.id}" value="${escapeDashboardHtml(client.tag || "")}" maxlength="200"></td>
      <td><span class="mono">${escapeDashboardHtml(client.id.slice(0, 8))}</span></td>
    </tr>`;
}

async function openBookingAction(action, bookingId) {
  const booking = await requestJson(dashboardApi.booking(bookingId));
  dashboardEls.actionForm.onsubmit = async (event) => {
    event.preventDefault();
    await submitBookingAction(action, bookingId);
  };

  if (action === "confirm") {
    dashboardEls.dialogTitle.textContent = "Подтвердить бронь";
    dashboardEls.dialogBody.innerHTML = `
      <div class="two-column">
        <div class="field-group"><label for="confirm-date">Дата приема</label><input id="confirm-date" type="date" value="${booking.confirmedDate || booking.requestedDate}" required></div>
        <div class="field-group"><label for="confirm-time">Время приема</label><input id="confirm-time" type="time" value="${(booking.confirmedTime || booking.requestedTime).slice(0, 5)}" required></div>
      </div>`;
  } else if (action === "reject") {
    dashboardEls.dialogTitle.textContent = "Отклонить заявку";
    dashboardEls.dialogBody.innerHTML = `<div class="field-group"><label for="reject-reason">Причина</label><textarea id="reject-reason" maxlength="500" rows="5">${escapeDashboardHtml(booking.rejectionReason || "")}</textarea></div>`;
  } else if (action === "complete") {
    dashboardEls.dialogTitle.textContent = "Отметить выполненной";
    dashboardEls.dialogBody.innerHTML = `<div class="field-group"><label for="actual-revenue">Фактическая сумма</label><input id="actual-revenue" type="number" min="0" step="0.01" value="${booking.actualRevenue ?? booking.totalPrice}" required></div>`;
  } else {
    dashboardEls.dialogTitle.textContent = "Ответить клиенту";
    dashboardEls.dialogBody.innerHTML = `<div class="field-group"><label for="specialist-reply">Ответ</label><textarea id="specialist-reply" maxlength="1000" rows="6" required>${escapeDashboardHtml(booking.specialistReply || "")}</textarea></div>`;
  }

  dashboardEls.dialog.showModal();
}

async function submitBookingAction(action, bookingId) {
  const options = {
    confirm: {
      url: dashboardApi.confirm(bookingId),
      method: "PUT",
      body: {
        date: document.querySelector("#confirm-date")?.value,
        time: normalizeTime(document.querySelector("#confirm-time")?.value)
      }
    },
    reject: {
      url: dashboardApi.reject(bookingId),
      method: "PUT",
      body: { reason: document.querySelector("#reject-reason")?.value || null }
    },
    complete: {
      url: dashboardApi.complete(bookingId),
      method: "PUT",
      body: { actualRevenue: Number(document.querySelector("#actual-revenue")?.value || 0) }
    },
    reply: {
      url: dashboardApi.reply(bookingId),
      method: "POST",
      body: { reply: document.querySelector("#specialist-reply")?.value || "" }
    }
  }[action];

  try {
    await requestJson(options.url, options);
    closeDialog();
    dashboardState.cache.clear();
    await renderBookings(true);
    showDashboardToast("Изменения сохранены.", false);
  } catch (error) {
    showDashboardToast(error.message, true);
  }
}

async function updateClientStatus(clientId, status) {
  await requestJson(dashboardApi.clientStatus(clientId), { method: "PUT", body: { status } });
  dashboardState.cache.delete("clients");
  showDashboardToast("Статус клиента обновлен.", false);
}

async function updateClientTag(clientId, tag) {
  await requestJson(dashboardApi.clientTag(clientId), { method: "PUT", body: { tag: tag.trim() || null } });
  dashboardState.cache.delete("clients");
  showDashboardToast("Метка клиента обновлена.", false);
}

function closeDialog() {
  dashboardEls.dialog.close();
  dashboardEls.actionForm.onsubmit = null;
}

async function getCached(key, url, force) {
  if (!force && dashboardState.cache.has(key)) {
    return dashboardState.cache.get(key);
  }

  const data = await requestJson(url);
  dashboardState.cache.set(key, data);
  return data;
}

async function requestJson(url, options = {}) {
  const headers = { "Content-Type": "application/json" };
  if (options.auth !== false && dashboardState.token) {
    headers.Authorization = `Bearer ${dashboardState.token}`;
  }

  const response = await fetch(url, {
    method: options.method || "GET",
    headers,
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

function statusText(status) {
  return ({ 1: "Новая", 2: "Подтверждена", 3: "Отклонена", 4: "Выполнена" })[status] || status;
}

function formatDateTime(value) {
  return new Intl.DateTimeFormat("ru-RU", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}

function formatDashboardMoney(value) {
  return new Intl.NumberFormat("ru-RU", { style: "currency", currency: "RUB", maximumFractionDigits: 0 }).format(value);
}

function normalizeTime(value) {
  return value && value.length === 5 ? `${value}:00` : value;
}

function showDashboardToast(message, isError) {
  dashboardEls.toast.textContent = message;
  dashboardEls.toast.classList.toggle("visible", Boolean(message));
  dashboardEls.toast.classList.toggle("error", isError);
  if (message) {
    setTimeout(() => dashboardEls.toast.classList.remove("visible"), 3200);
  }
}

function debounce(fn, delay) {
  let timeoutId;
  return () => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(fn, delay);
  };
}

function escapeDashboardHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
