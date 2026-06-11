const adminApi = {
  login: "/api/v1/admin/auth/login",
  me: "/api/v1/admin/auth/me",
  specialists: "/api/v1/admin/specialists",
  specialist: (id) => `/api/v1/admin/specialists/${encodeURIComponent(id)}`,
  specialistBlock: (id) => `/api/v1/admin/specialists/${encodeURIComponent(id)}/block`,
  specialistUnblock: (id) => `/api/v1/admin/specialists/${encodeURIComponent(id)}/unblock`,
  specialistPlan: (id) => `/api/v1/admin/specialists/${encodeURIComponent(id)}/plan`,
  bookings: "/api/v1/admin/bookings",
  booking: (id) => `/api/v1/admin/bookings/${encodeURIComponent(id)}`,
  bookingStatus: (id) => `/api/v1/admin/bookings/${encodeURIComponent(id)}/status`,
  clients: "/api/v1/admin/clients",
  client: (id) => `/api/v1/admin/clients/${encodeURIComponent(id)}`,
  services: "/api/v1/admin/services",
  service: (id) => `/api/v1/admin/services/${encodeURIComponent(id)}`,
  locations: "/api/v1/admin/locations",
  location: (id) => `/api/v1/admin/locations/${encodeURIComponent(id)}`,
  payments: "/api/v1/admin/payments",
  payment: (id) => `/api/v1/admin/payments/${encodeURIComponent(id)}`,
  paymentWebhook: "/api/v1/admin/payments/webhook",
  financeSummary: "/api/v1/admin/payments/summary",
  subscriptions: "/api/v1/admin/subscriptions",
  subscriptionStatus: (id) => `/api/v1/admin/subscriptions/${encodeURIComponent(id)}/status`,
  subscriptionRenew: (id) => `/api/v1/admin/subscriptions/${encodeURIComponent(id)}/renew`,
  auditLogs: "/api/v1/admin/audit-logs",
  auditCsv: "/api/v1/admin/audit-logs/export.csv",
  settings: "/api/v1/admin/settings",
  setting: (id) => `/api/v1/admin/settings/${encodeURIComponent(id)}`,
  admins: "/api/v1/admin/admins",
  admin: (id) => `/api/v1/admin/admins/${encodeURIComponent(id)}`,
  plans: "/api/v1/subscription-plans",
  plan: (id) => `/api/v1/subscription-plans/${encodeURIComponent(id)}`
};

const adminState = {
  route: "specialists",
  token: getStoredAdminAuthValue("serviceBookingAdminAccessToken") || "",
  role: getStoredAdminAuthValue("serviceBookingUserRole") || "",
  cache: new Map(),
  financeChart: null,
  refreshTimerId: 0
};

const adminMenuItems = [
  { route: "specialists", label: "Специалисты", icon: "users" },
  { route: "bookings", label: "Заявки", icon: "inbox" },
  { route: "clients", label: "Клиенты", icon: "user" },
  { route: "catalogs", label: "Справочники", icon: "list" },
  { route: "finance", label: "Финансы", icon: "chart" },
  { route: "audit", label: "Аудит", icon: "file" },
  { route: "settings", label: "Настройки", icon: "settings" }
];

const adminEls = {
  sidebar: document.querySelector("#admin-sidebar"),
  nav: document.querySelector("#admin-nav"),
  content: document.querySelector("#admin-content"),
  toast: document.querySelector("#admin-toast")
};

document.addEventListener("DOMContentLoaded", initAdmin);

function initAdmin() {
  window.addEventListener("popstate", () => {
    adminState.route = getAdminRouteFromLocation();
    loadAdminRoute(false);
  });
  adminState.route = getAdminRouteFromLocation();
  if (!adminState.token || adminState.role !== "Admin") {
    redirectToAdminLogin();
    return;
  }

  buildAdminMenu();
  loadAdminRoute(false);
  startAdminAutoRefresh();
}

function getAdminRouteFromLocation() {
  const route = new URLSearchParams(location.search).get("section");
  return adminMenuItems.some((item) => item.route === route)
    ? route
    : "specialists";
}

function navigateAdmin(route) {
  adminState.route = route;
  history.pushState({}, "", `/admin.html?section=${route}`);
  loadAdminRoute(false);
}

function logoutAdmin() {
  adminState.token = "";
  adminState.cache.clear();
  localStorage.removeItem("serviceBookingAdminAccessToken");
  localStorage.removeItem("serviceBookingUserRole");
  localStorage.removeItem("serviceBookingAccessToken");
  localStorage.removeItem("serviceBookingRefreshToken");
  sessionStorage.removeItem("serviceBookingAdminAccessToken");
  sessionStorage.removeItem("serviceBookingUserRole");
  sessionStorage.removeItem("serviceBookingAccessToken");
  sessionStorage.removeItem("serviceBookingRefreshToken");
  redirectToAdminLogin();
}

async function loadAdminRoute(force) {
  updateAdminChrome();
  if (!adminState.token) {
    redirectToAdminLogin();
    return;
  }

  destroyFinanceChart();
  adminEls.content.classList.add("is-transitioning");
  setTimeout(async () => {
    try {
      if (adminState.route === "bookings") await renderAdminBookings(force);
      else if (adminState.route === "clients") await renderAdminClients(force);
      else if (adminState.route === "catalogs") await renderAdminCatalogs(force);
      else if (adminState.route === "finance") await renderAdminFinance(force);
      else if (adminState.route === "audit") await renderAdminAudit(force);
      else if (adminState.route === "settings") await renderAdminSettings(force);
      else await renderAdminSpecialists(force);
      adminEls.content.classList.remove("is-transitioning");
    } catch (error) {
      adminEls.content.classList.remove("is-transitioning");
      if (error.status === 401 || error.status === 403) logoutAdmin();
      else adminEls.content.innerHTML = `<div class="empty-state">${escapeAdminHtml(error.message)}</div>`;
    }
  }, 180);
}

function startAdminAutoRefresh() {
  window.clearInterval(adminState.refreshTimerId);
  adminState.refreshTimerId = window.setInterval(() => {
    if (adminState.route === "settings") {
      return;
    }
    loadAdminRoute(true);
  }, 30000);
}

function buildAdminMenu() {
  adminEls.nav.innerHTML = adminMenuItems
    .map((item) => `
      <button type="button" class="dashboard-nav-item app-nav-item" data-admin-route="${item.route}" aria-label="${escapeAdminHtml(item.label)}">
        ${adminNavIcon(item.icon)}
        <span>${escapeAdminHtml(item.label)}</span>
      </button>`)
    .join("");
  adminEls.nav.querySelectorAll("[data-admin-route]").forEach((button) => {
    button.addEventListener("click", () => navigateAdmin(button.dataset.adminRoute));
  });
  adminEls.sidebar.hidden = false;
  document.body.classList.remove("no-sidebar");
  updateAdminChrome();
}

function redirectToAdminLogin() {
  location.href = "/login";
}

function getStoredAdminAuthValue(key) {
  return localStorage.getItem(key) || sessionStorage.getItem(key);
}

function updateAdminChrome() {
  adminEls.nav.querySelectorAll("[data-admin-route]").forEach((button) => {
    button.classList.toggle("active", button.dataset.adminRoute === adminState.route);
  });
}

async function renderAdminSpecialists(force) {
  adminEls.content.innerHTML = loadingHtml();
  const [specialists, plans] = await Promise.all([
    adminCached("specialists", adminApi.specialists, force),
    adminCached("plans", adminApi.plans, force)
  ]);
  adminEls.content.innerHTML = `
    <div class="admin-panel">
      <div class="data-table-wrap">
        <table class="data-table admin-table">
          <thead><tr><th>Специалист</th><th>Контакты</th><th>Тариф</th><th>Статус</th><th>Действия</th></tr></thead>
          <tbody>${specialists.map((item) => `
            <tr>
              <td>${escapeAdminHtml(item.fullName)}<span class="muted-cell">${escapeAdminHtml(item.venueName || "-")}</span></td>
              <td>${escapeAdminHtml(item.email)}<span class="muted-cell">${escapeAdminHtml(item.phone)}</span></td>
              <td>
                <select data-plan-specialist="${item.id}">
                  <option value="">Без тарифа</option>
                  ${plans.map((plan) => `<option value="${plan.id}"${plan.name === item.subscriptionPlanName ? " selected" : ""}>${escapeAdminHtml(plan.name)}</option>`).join("")}
                </select>
              </td>
              <td><span class="status-badge ${item.isBlocked ? "status-rejected" : "status-confirmed"}">${item.isBlocked ? "Заблокирован" : "Активен"}</span></td>
              <td>
                <div class="row-actions">
                  <button type="button" class="table-action" data-toggle-specialist="${item.id}" data-blocked="${item.isBlocked}">${item.isBlocked ? "Разблокировать" : "Блокировать"}</button>
                  <button type="button" class="table-action" data-delete-specialist="${item.id}">Удалить</button>
                </div>
              </td>
            </tr>`).join("") || `<tr><td colspan="5">Специалисты не найдены.</td></tr>`}</tbody>
        </table>
      </div>
    </div>`;
  adminEls.content.querySelectorAll("[data-plan-specialist]").forEach((select) => {
    select.addEventListener("change", () => changeSpecialistPlan(select.dataset.planSpecialist, select.value));
  });
  adminEls.content.querySelectorAll("[data-toggle-specialist]").forEach((button) => {
    button.addEventListener("click", () => toggleSpecialistBlock(button.dataset.toggleSpecialist, button.dataset.blocked === "true"));
  });
  adminEls.content.querySelectorAll("[data-delete-specialist]").forEach((button) => {
    button.addEventListener("click", () => deleteAdminEntity(adminApi.specialist(button.dataset.deleteSpecialist), "specialists", renderAdminSpecialists));
  });
}

async function changeSpecialistPlan(specialistId, planId) {
  if (!planId) return;
  await adminRequest(adminApi.specialistPlan(specialistId), { method: "PUT", body: { planId, expiresAt: addAdminDaysIso(todayAdminIso(), 30) } });
  adminState.cache.delete("specialists");
  showAdminToast("Тариф специалиста обновлен.", false);
  await renderAdminSpecialists(true);
}

async function toggleSpecialistBlock(specialistId, isBlocked) {
  const url = isBlocked ? adminApi.specialistUnblock(specialistId) : adminApi.specialistBlock(specialistId);
  await adminRequest(url, { method: "PUT", body: isBlocked ? undefined : { reason: "Blocked by admin" } });
  adminState.cache.delete("specialists");
  await renderAdminSpecialists(true);
}

async function renderAdminBookings(force) {
  adminEls.content.innerHTML = loadingHtml();
  const bookings = await adminCached("bookings", adminApi.bookings, force);
  adminEls.content.innerHTML = `
    <div class="data-table-wrap">
      <table class="data-table admin-table">
        <thead><tr><th>ID</th><th>Специалист</th><th>Клиент</th><th>Дата</th><th>Услуги</th><th>Сумма</th><th>Статус</th><th></th></tr></thead>
        <tbody>${bookings.map((item) => `
          <tr>
            <td><span class="mono">${escapeAdminHtml(item.id.slice(0, 8))}</span></td>
            <td>${escapeAdminHtml(item.specialistName || "-")}</td>
            <td>${escapeAdminHtml(item.clientName)}<span class="muted-cell">${escapeAdminHtml(item.clientPhone)}</span></td>
            <td>${escapeAdminHtml(item.requestedDate)} ${escapeAdminHtml(item.requestedTime.slice(0, 5))}</td>
            <td>${escapeAdminHtml(item.servicesSummary || "Без услуги")}</td>
            <td>${formatAdminMoney(item.totalPrice)}</td>
            <td><select data-booking-status="${item.id}">${statusOptions(item.status)}</select></td>
            <td><button type="button" class="table-action" data-delete-booking="${item.id}">Удалить</button></td>
          </tr>`).join("") || `<tr><td colspan="8">Заявки не найдены.</td></tr>`}</tbody>
      </table>
    </div>`;
  adminEls.content.querySelectorAll("[data-booking-status]").forEach((select) => {
    select.addEventListener("change", () => changeBookingStatus(select.dataset.bookingStatus, Number(select.value)));
  });
  adminEls.content.querySelectorAll("[data-delete-booking]").forEach((button) => {
    button.addEventListener("click", () => deleteAdminEntity(adminApi.booking(button.dataset.deleteBooking), "bookings", renderAdminBookings));
  });
}

async function changeBookingStatus(bookingId, status) {
  await adminRequest(adminApi.bookingStatus(bookingId), { method: "PUT", body: { status } });
  adminState.cache.delete("bookings");
  showAdminToast("Статус заявки обновлен.", false);
}

async function renderAdminClients(force) {
  adminEls.content.innerHTML = loadingHtml();
  const clients = await adminCached("clients", adminApi.clients, force);
  adminEls.content.innerHTML = `
    <div class="data-table-wrap">
      <table class="data-table admin-table">
        <thead><tr><th>Клиент</th><th>Телефон</th><th>Заявки</th><th>Статус</th><th>Метка</th><th>Действия</th></tr></thead>
        <tbody>${clients.map((item) => `
          <tr>
            <td><input data-client-name="${item.id}" value="${escapeAdminHtml(item.fullName)}"></td>
            <td><input data-client-phone="${item.id}" value="${escapeAdminHtml(item.phone)}"></td>
            <td>${item.bookingCount}</td>
            <td><select data-client-status="${item.id}">
              <option value="1"${item.status === 1 ? " selected" : ""}>Обычный</option>
              <option value="2"${item.status === 2 ? " selected" : ""}>VIP</option>
              <option value="3"${item.status === 3 ? " selected" : ""}>Забанен</option>
            </select></td>
            <td><input data-client-tag="${item.id}" value="${escapeAdminHtml(item.tag || "")}"></td>
            <td><button type="button" class="table-action" data-save-client="${item.id}">Сохранить</button></td>
          </tr>`).join("") || `<tr><td colspan="6">Клиенты не найдены.</td></tr>`}</tbody>
      </table>
    </div>`;
  adminEls.content.querySelectorAll("[data-save-client]").forEach((button) => {
    button.addEventListener("click", () => saveAdminClient(button.dataset.saveClient));
  });
}

async function saveAdminClient(clientId) {
  await adminRequest(adminApi.client(clientId), {
    method: "PUT",
    body: {
      fullName: document.querySelector(`[data-client-name="${clientId}"]`).value.trim(),
      phone: document.querySelector(`[data-client-phone="${clientId}"]`).value.trim(),
      status: Number(document.querySelector(`[data-client-status="${clientId}"]`).value),
      tag: document.querySelector(`[data-client-tag="${clientId}"]`).value.trim() || null
    }
  });
  adminState.cache.delete("clients");
  showAdminToast("Клиент сохранен.", false);
}

async function renderAdminCatalogs(force) {
  adminEls.content.innerHTML = loadingHtml();
  const [services, locations] = await Promise.all([
    adminCached("admin-services", adminApi.services, force),
    adminCached("admin-locations", adminApi.locations, force)
  ]);
  adminEls.content.innerHTML = `
    <div class="admin-grid">
      <section class="admin-panel">
        <div class="section-title"><h2>Услуги</h2></div>
        <form class="admin-inline-form" id="admin-service-form">
          <input id="admin-service-name" placeholder="Название услуги" required>
          <input id="admin-service-description" placeholder="Описание">
          <button class="primary-button" type="submit">Добавить</button>
        </form>
        ${simpleCatalogTable(services, "service")}
      </section>
      <section class="admin-panel">
        <div class="section-title"><h2>Локации</h2></div>
        <form class="admin-inline-form" id="admin-location-form">
          <input id="admin-location-name" placeholder="Название" required>
          <input id="admin-location-address" placeholder="Адрес" required>
          <input id="admin-location-description" placeholder="Описание">
          <button class="primary-button" type="submit">Добавить</button>
        </form>
        ${simpleCatalogTable(locations, "location")}
      </section>
    </div>`;
  document.querySelector("#admin-service-form").addEventListener("submit", createAdminService);
  document.querySelector("#admin-location-form").addEventListener("submit", createAdminLocation);
  bindCatalogDeletes();
}

function simpleCatalogTable(items, type) {
  return `
    <div class="data-table-wrap admin-small-table">
      <table class="data-table">
        <thead><tr><th>Название</th><th>Описание</th><th></th></tr></thead>
        <tbody>${items.map((item) => `
          <tr>
            <td>${escapeAdminHtml(item.name)}</td>
            <td>${escapeAdminHtml(item.address || item.description || "-")}</td>
            <td><button type="button" class="table-action" data-delete-${type}="${item.id}">Удалить</button></td>
          </tr>`).join("") || `<tr><td colspan="3">Нет данных.</td></tr>`}</tbody>
      </table>
    </div>`;
}

async function createAdminService(event) {
  event.preventDefault();
  await adminRequest(adminApi.services, {
    method: "POST",
    body: {
      name: document.querySelector("#admin-service-name").value.trim(),
      description: document.querySelector("#admin-service-description").value.trim() || null
    }
  });
  adminState.cache.delete("admin-services");
  await renderAdminCatalogs(true);
}

async function createAdminLocation(event) {
  event.preventDefault();
  await adminRequest(adminApi.locations, {
    method: "POST",
    body: {
      name: document.querySelector("#admin-location-name").value.trim(),
      address: document.querySelector("#admin-location-address").value.trim(),
      description: document.querySelector("#admin-location-description").value.trim() || null
    }
  });
  adminState.cache.delete("admin-locations");
  await renderAdminCatalogs(true);
}

function bindCatalogDeletes() {
  adminEls.content.querySelectorAll("[data-delete-service]").forEach((button) => {
    button.addEventListener("click", () => deleteAdminEntity(adminApi.service(button.dataset.deleteService), "admin-services", renderAdminCatalogs));
  });
  adminEls.content.querySelectorAll("[data-delete-location]").forEach((button) => {
    button.addEventListener("click", () => deleteAdminEntity(adminApi.location(button.dataset.deleteLocation), "admin-locations", renderAdminCatalogs));
  });
}

async function renderAdminFinance(force) {
  adminEls.content.innerHTML = loadingHtml();
  const [payments, summary, subscriptions, specialists] = await Promise.all([
    adminCached("payments", adminApi.payments, force),
    adminCached("finance-summary", adminApi.financeSummary, force),
    adminCached("subscriptions", adminApi.subscriptions, force),
    adminCached("specialists", adminApi.specialists, force)
  ]);
  adminEls.content.innerHTML = `
    <div class="summary-grid">
      <article class="summary-card"><span>MRR</span><strong>${formatAdminMoney(summary.mrr)}</strong></article>
      <article class="summary-card"><span>ARPU</span><strong>${formatAdminMoney(summary.arpu)}</strong></article>
      <article class="summary-card"><span>Доход всего</span><strong>${formatAdminMoney(summary.totalRevenue)}</strong></article>
    </div>
    <section class="admin-panel">
      <form class="admin-inline-form" id="admin-payment-form">
        <select id="admin-payment-specialist">${specialists.map((item) => `<option value="${item.id}">${escapeAdminHtml(item.fullName)}</option>`).join("")}</select>
        <input id="admin-payment-amount" type="number" min="1" step="0.01" placeholder="Сумма" required>
        <button class="primary-button" type="submit">Создать платеж</button>
      </form>
      <div class="chart-panel admin-finance-chart"><header>Транзакции</header><canvas id="admin-finance-chart"></canvas></div>
    </section>
    <div class="data-table-wrap">
      <table class="data-table admin-table">
        <thead><tr><th>Платеж</th><th>Специалист</th><th>Сумма</th><th>Статус</th><th>Дата</th><th></th></tr></thead>
        <tbody>${payments.map((item) => `
          <tr>
            <td><span class="mono">${escapeAdminHtml(item.id.slice(0, 8))}</span></td>
            <td>${escapeAdminHtml(item.specialistName || "-")}</td>
            <td>${formatAdminMoney(item.amount)}</td>
            <td>${escapeAdminHtml(paymentStatusText(item.status))}</td>
            <td>${formatAdminDateTime(item.createdAt)}</td>
            <td>${item.status === 1 ? `<button type="button" class="table-action" data-pay-success="${item.id}">Успешно</button>` : ""}</td>
          </tr>`).join("") || `<tr><td colspan="6">Транзакции не найдены.</td></tr>`}</tbody>
      </table>
    </div>
    <div class="data-table-wrap">
      <table class="data-table admin-table">
        <thead><tr><th>Подписка</th><th>Специалист</th><th>План</th><th>Статус</th><th>До</th></tr></thead>
        <tbody>${subscriptions.map((item) => `
          <tr><td><span class="mono">${escapeAdminHtml(item.id.slice(0, 8))}</span></td><td>${escapeAdminHtml(item.specialistName)}</td><td>${escapeAdminHtml(item.planName)}</td><td>${escapeAdminHtml(subscriptionStatusText(item.status))}</td><td>${formatAdminDateTime(item.expiresAt)}</td></tr>`).join("") || `<tr><td colspan="5">Подписки не найдены.</td></tr>`}</tbody>
      </table>
    </div>`;
  document.querySelector("#admin-payment-form").addEventListener("submit", createAdminPayment);
  adminEls.content.querySelectorAll("[data-pay-success]").forEach((button) => {
    button.addEventListener("click", () => markPaymentSuccess(button.dataset.paySuccess));
  });
  renderFinanceChart(payments);
}

async function createAdminPayment(event) {
  event.preventDefault();
  await adminRequest(adminApi.payments, {
    method: "POST",
    body: {
      specialistId: document.querySelector("#admin-payment-specialist").value,
      amount: Number(document.querySelector("#admin-payment-amount").value),
      currency: "RUB"
    }
  });
  adminState.cache.delete("payments");
  await renderAdminFinance(true);
}

async function markPaymentSuccess(paymentId) {
  await adminRequest(adminApi.paymentWebhook, { method: "POST", auth: false, body: { paymentId, status: 2, externalId: `mock-${Date.now()}` } });
  adminState.cache.delete("payments");
  adminState.cache.delete("finance-summary");
  await renderAdminFinance(true);
}

function renderFinanceChart(payments) {
  if (!window.Chart) return;
  const groups = new Map();
  payments.forEach((payment) => {
    const date = String(payment.createdAt).slice(0, 10);
    groups.set(date, (groups.get(date) || 0) + Number(payment.amount || 0));
  });
  adminState.financeChart = new Chart(document.querySelector("#admin-finance-chart"), {
    type: "bar",
    data: {
      labels: [...groups.keys()],
      datasets: [{ label: "Платежи", data: [...groups.values()], backgroundColor: "#146c5f" }]
    },
    options: { responsive: true, maintainAspectRatio: false, scales: { y: { beginAtZero: true } } }
  });
}

async function renderAdminAudit(force) {
  adminEls.content.innerHTML = loadingHtml();
  const logs = await adminCached("audit", adminApi.auditLogs, force);
  adminEls.content.innerHTML = `
    <div class="table-toolbar admin-audit-toolbar">
      <button type="button" class="secondary-button" id="admin-audit-export">CSV</button>
    </div>
    <div class="data-table-wrap">
      <table class="data-table admin-table">
        <thead><tr><th>Дата</th><th>Актор</th><th>Действие</th><th>Сущность</th><th>Результат</th><th>Детали</th></tr></thead>
        <tbody>${logs.map((item) => `
          <tr>
            <td>${formatAdminDateTime(item.createdAt)}</td>
            <td>${escapeAdminHtml(item.actorType)}<span class="muted-cell">${escapeAdminHtml(item.actorId || "-")}</span></td>
            <td>${escapeAdminHtml(item.action)}</td>
            <td>${escapeAdminHtml(item.entityType)}<span class="muted-cell">${escapeAdminHtml(item.entityId || "-")}</span></td>
            <td>${escapeAdminHtml(item.outcome)}</td>
            <td>${escapeAdminHtml(item.details || "-")}</td>
          </tr>`).join("") || `<tr><td colspan="6">Журнал пуст.</td></tr>`}</tbody>
      </table>
    </div>`;
  document.querySelector("#admin-audit-export").addEventListener("click", exportAuditCsv);
}

async function renderAdminSettings(force) {
  adminEls.content.innerHTML = loadingHtml();
  const [settings, admins, plans] = await Promise.all([
    adminCached("settings", adminApi.settings, force),
    adminCached("admins", adminApi.admins, force),
    adminCached("plans", adminApi.plans, force)
  ]);
  adminEls.content.innerHTML = `
    <div class="admin-grid">
      <section class="admin-panel admin-session-panel">
        <div class="section-title"><h2>Сессия</h2></div>
        <button type="button" class="secondary-button" data-admin-logout>Выйти</button>
      </section>
      <section class="admin-panel">
        <div class="section-title"><h2>Системные параметры</h2></div>
        <form class="admin-inline-form" id="admin-setting-form">
          <input id="setting-key" placeholder="Ключ" required>
          <input id="setting-value" placeholder="Значение" required>
          <input id="setting-description" placeholder="Описание">
          <button class="primary-button" type="submit">Сохранить</button>
        </form>
        ${settingsList(settings)}
      </section>
      <section class="admin-panel">
        <div class="section-title"><h2>Администраторы</h2></div>
        <form class="admin-inline-form" id="admin-user-form">
          <input id="admin-user-name" placeholder="ФИО" required>
          <input id="admin-user-email" type="email" placeholder="Email" required>
          <input id="admin-user-password" type="password" placeholder="Пароль" required>
          <button class="primary-button" type="submit">Добавить</button>
        </form>
        ${adminsList(admins)}
      </section>
      <section class="admin-panel admin-wide-panel">
        <div class="section-title"><h2>Тарифы</h2></div>
        <form class="admin-inline-form" id="admin-plan-form">
          <input id="plan-name" placeholder="Название" required>
          <input id="plan-price" type="number" min="0" step="0.01" placeholder="Цена" required>
          <input id="plan-bookings" type="number" min="0" step="1" placeholder="Лимит заявок" required>
          <input id="plan-services" type="number" min="0" step="1" placeholder="Лимит услуг" required>
          <button class="primary-button" type="submit">Добавить</button>
        </form>
        ${plansList(plans)}
      </section>
    </div>`;
  adminEls.content.querySelector("[data-admin-logout]").addEventListener("click", logoutAdmin);
  document.querySelector("#admin-setting-form").addEventListener("submit", upsertSetting);
  document.querySelector("#admin-user-form").addEventListener("submit", createAdminUser);
  document.querySelector("#admin-plan-form").addEventListener("submit", createPlan);
}

function settingsList(settings) {
  return `<div class="admin-list">${settings.map((item) => `<div class="admin-list-row"><strong>${escapeAdminHtml(item.key)}</strong><span>${escapeAdminHtml(item.value)}</span></div>`).join("") || `<div class="empty-state">Нет параметров.</div>`}</div>`;
}

function adminsList(admins) {
  return `<div class="admin-list">${admins.map((item) => `<div class="admin-list-row"><strong>${escapeAdminHtml(item.fullName)}</strong><span>${escapeAdminHtml(item.email)}</span></div>`).join("") || `<div class="empty-state">Нет администраторов.</div>`}</div>`;
}

function plansList(plans) {
  return `<div class="admin-list">${plans.map((item) => `<div class="admin-list-row"><strong>${escapeAdminHtml(item.name)}</strong><span>${formatAdminMoney(item.monthlyPrice)} · заявки ${item.bookingLimit || "∞"} · услуги ${item.serviceLimit || "∞"}</span></div>`).join("") || `<div class="empty-state">Нет тарифов.</div>`}</div>`;
}

async function upsertSetting(event) {
  event.preventDefault();
  await adminRequest(adminApi.settings, {
    method: "PUT",
    body: {
      key: document.querySelector("#setting-key").value.trim(),
      value: document.querySelector("#setting-value").value.trim(),
      description: document.querySelector("#setting-description").value.trim() || null
    }
  });
  adminState.cache.delete("settings");
  await renderAdminSettings(true);
}

async function createAdminUser(event) {
  event.preventDefault();
  await adminRequest(adminApi.admins, {
    method: "POST",
    body: {
      fullName: document.querySelector("#admin-user-name").value.trim(),
      email: document.querySelector("#admin-user-email").value.trim(),
      password: document.querySelector("#admin-user-password").value,
      isActive: true
    }
  });
  adminState.cache.delete("admins");
  await renderAdminSettings(true);
}

async function createPlan(event) {
  event.preventDefault();
  await adminRequest(adminApi.plans, {
    method: "POST",
    body: {
      name: document.querySelector("#plan-name").value.trim(),
      description: null,
      monthlyPrice: Number(document.querySelector("#plan-price").value),
      bookingLimit: Number(document.querySelector("#plan-bookings").value),
      serviceLimit: Number(document.querySelector("#plan-services").value),
      isActive: true
    }
  });
  adminState.cache.delete("plans");
  await renderAdminSettings(true);
}

async function deleteAdminEntity(url, cacheKey, renderer) {
  if (!window.confirm("Удалить запись?")) return;
  await adminRequest(url, { method: "DELETE" });
  adminState.cache.delete(cacheKey);
  await renderer(true);
}

async function exportAuditCsv() {
  const response = await fetch(adminApi.auditCsv, { headers: { Authorization: `Bearer ${adminState.token}` } });
  if (!response.ok) {
    showAdminToast(`CSV не сформирован: HTTP ${response.status}`, true);
    return;
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "audit-logs.csv";
  link.click();
  URL.revokeObjectURL(url);
}

async function adminCached(key, url, force) {
  if (!force && adminState.cache.has(key)) return adminState.cache.get(key);
  const data = await adminRequest(url);
  adminState.cache.set(key, data);
  return data;
}

async function adminRequest(url, options = {}) {
  const headers = { "Content-Type": "application/json" };
  if (options.auth !== false && adminState.token) headers.Authorization = `Bearer ${adminState.token}`;
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

function statusOptions(current) {
  return [
    [1, "Новая"],
    [2, "Подтверждена"],
    [3, "Отклонена"],
    [4, "Выполнена"]
  ].map(([value, label]) => `<option value="${value}"${Number(current) === value ? " selected" : ""}>${label}</option>`).join("");
}

function paymentStatusText(status) {
  return ({ 1: "Ожидает", 2: "Успешно", 3: "Ошибка", 4: "Возврат" })[status] || status;
}

function subscriptionStatusText(status) {
  return ({ 1: "Пробный", 2: "Активна", 3: "Просрочена", 4: "Заморожена", 5: "Отменена", 6: "Истекла" })[status] || status;
}

function loadingHtml() {
  return `<div class="empty-state">Загрузка...</div>`;
}

function showAdminToast(message, isError) {
  adminEls.toast.textContent = message;
  adminEls.toast.classList.toggle("visible", Boolean(message));
  adminEls.toast.classList.toggle("error", isError);
  if (message) setTimeout(() => adminEls.toast.classList.remove("visible"), 3200);
}

function destroyFinanceChart() {
  if (adminState.financeChart) {
    adminState.financeChart.destroy();
    adminState.financeChart = null;
  }
}

function todayAdminIso() {
  return formatAdminIsoDate(new Date());
}

function addAdminDaysIso(value, days) {
  const date = new Date(`${value}T00:00:00`);
  date.setDate(date.getDate() + days);
  return formatAdminIsoDate(date);
}

function formatAdminIsoDate(date) {
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

function formatAdminDateTime(value) {
  return new Intl.DateTimeFormat("ru-RU", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}

function formatAdminMoney(value) {
  return new Intl.NumberFormat("ru-RU", { style: "currency", currency: "RUB", maximumFractionDigits: 2 }).format(Number(value) || 0);
}

function escapeAdminHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function adminNavIcon(name) {
  const icons = {
    users: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></svg>`,
    inbox: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M22 12h-6l-2 3h-4l-2-3H2M5 4h14l3 8v6a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2v-6Z"/></svg>`,
    user: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 21a8 8 0 0 0-16 0M12 13a5 5 0 1 0 0-10 5 5 0 0 0 0 10Z"/></svg>`,
    list: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/></svg>`,
    chart: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 3v18h18M8 17V9M13 17V5M18 17v-6"/></svg>`,
    file: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8ZM14 2v6h6M8 13h8M8 17h6"/></svg>`,
    settings: `<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7ZM19.4 15a1.7 1.7 0 0 0 .34 1.88l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06A1.7 1.7 0 0 0 15 19.4a1.7 1.7 0 0 0-1 1.55V21a2 2 0 1 1-4 0v-.09a1.7 1.7 0 0 0-1-1.55 1.7 1.7 0 0 0-1.88.34l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.7 1.7 0 0 0 4.6 15a1.7 1.7 0 0 0-1.55-1H3a2 2 0 1 1 0-4h.09a1.7 1.7 0 0 0 1.55-1 1.7 1.7 0 0 0-.34-1.88l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.7 1.7 0 0 0 9 4.6a1.7 1.7 0 0 0 1-1.55V3a2 2 0 1 1 4 0v.09a1.7 1.7 0 0 0 1 1.55 1.7 1.7 0 0 0 1.88-.34l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.7 1.7 0 0 0 19.4 9c.22.62.82 1 1.55 1H21a2 2 0 1 1 0 4h-.09a1.7 1.7 0 0 0-1.55 1Z"/></svg>`
  };
  return icons[name] || "";
}
