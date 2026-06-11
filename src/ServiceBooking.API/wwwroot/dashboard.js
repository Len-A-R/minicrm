const dashboardApi = {
  login: "/api/v1/auth/login",
  profile: "/api/v1/profile",
  locations: "/api/v1/locations",
  bookings: "/api/v1/specialist/bookings",
  booking: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}`,
  confirm: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}/confirm`,
  reject: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}/reject`,
  complete: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}/complete`,
  reply: (id) => `/api/v1/specialist/bookings/${encodeURIComponent(id)}/reply`,
  catalogServices: "/api/v1/services",
  specialistServices: "/api/v1/specialist-services",
  specialistService: (id) => `/api/v1/specialist-services/${encodeURIComponent(id)}`,
  clients: "/api/v1/specialist/clients",
  clientStatus: (id) => `/api/v1/specialist/clients/${encodeURIComponent(id)}/status`,
  clientTag: (id) => `/api/v1/specialist/clients/${encodeURIComponent(id)}/tag`,
  calendar: (from, to) => `/api/v1/calendar?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
  reschedule: (id) => `/api/v1/calendar/${encodeURIComponent(id)}/reschedule`,
  cancelCalendar: (id) => `/api/v1/calendar/${encodeURIComponent(id)}`,
  kanban: (date) => `/api/v1/kanban?date=${encodeURIComponent(date)}`,
  kanbanMove: (id) => `/api/v1/kanban/${encodeURIComponent(id)}/move`
};

const dashboardState = {
  route: "bookings",
  cache: new Map(),
  bookingFilters: { status: "", date: "", search: "" },
  editingSpecialistServiceId: "",
  calendarView: "month",
  calendarDate: todayIso(),
  kanbanDate: todayIso(),
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
  return ["profile", "bookings", "clients", "services", "calendar", "kanban"].includes(route) ? route : "bookings";
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
    localStorage.setItem("serviceBookingRefreshToken", auth.refreshToken);
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
  localStorage.removeItem("serviceBookingRefreshToken");
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
      if (dashboardState.route === "profile") {
        await renderProfile(force);
      } else if (dashboardState.route === "clients") {
        await renderClients(force);
      } else if (dashboardState.route === "services") {
        await renderSpecialistServices(force);
      } else if (dashboardState.route === "calendar") {
        await renderCalendar(force);
      } else if (dashboardState.route === "kanban") {
        await renderKanban(force);
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
  const titles = {
    profile: ["Профиль", "Профиль специалиста"],
    bookings: ["Заявки", "Управление заявками"],
    clients: ["Клиенты", "Клиентская база"],
    services: ["Услуги", "Предоставляемые услуги"],
    calendar: ["Календарь", "Расписание"],
    kanban: ["Kanban", "Доска заявок"]
  };
  const [label, title] = titles[dashboardState.route] || titles.bookings;
  dashboardEls.title.textContent = title;
  dashboardEls.label.textContent = label;
  dashboardEls.nav.forEach((button) => button.classList.toggle("active", button.dataset.route === dashboardState.route));
}

async function renderProfile(force) {
  dashboardEls.content.innerHTML = `<div class="empty-state">Загрузка...</div>`;
  const [profile, locations] = await Promise.all([
    getCached("profile", dashboardApi.profile, force),
    getCached("locations", dashboardApi.locations, force)
  ]);

  dashboardEls.content.innerHTML = `
    <form class="profile-editor" id="profile-form">
      <div class="section-title">
        <h2>Основные данные</h2>
        <span class="muted-cell">${escapeDashboardHtml(profile.email)}</span>
      </div>
      <div class="two-column profile-grid">
        <div class="field-group">
          <label for="profile-full-name">ФИО</label>
          <input id="profile-full-name" type="text" value="${escapeDashboardHtml(profile.fullName)}" required>
        </div>
        <div class="field-group">
          <label for="profile-phone">Телефон</label>
          <input id="profile-phone" type="tel" value="${escapeDashboardHtml(profile.phone)}" required>
        </div>
      </div>
      <div class="field-group">
        <label for="profile-venue-name">Название заведения</label>
        <input id="profile-venue-name" type="text" value="${escapeDashboardHtml(profile.venueName || "")}" maxlength="160">
      </div>

      <div class="section-title profile-section-title">
        <h2>Расположение</h2>
      </div>
      <div class="field-group">
        <label for="profile-location-id">Существующая локация</label>
        <select id="profile-location-id">
          <option value="">Не выбрано</option>
          ${locations.map((location) => `
            <option value="${location.id}"${location.id === profile.locationId ? " selected" : ""}>
              ${escapeDashboardHtml(location.name)} · ${escapeDashboardHtml(location.address)}
            </option>`).join("")}
        </select>
      </div>
      <div class="two-column profile-grid">
        <div class="field-group">
          <label for="profile-new-location-name">Новая локация</label>
          <input id="profile-new-location-name" type="text" maxlength="120" placeholder="Например: Центральный офис">
        </div>
        <div class="field-group">
          <label for="profile-new-location-address">Адрес новой локации</label>
          <input id="profile-new-location-address" type="text" maxlength="250">
        </div>
      </div>
      <div class="field-group">
        <label for="profile-new-location-description">Описание новой локации</label>
        <input id="profile-new-location-description" type="text" maxlength="500">
      </div>

      <button class="primary-button" type="submit">Сохранить профиль</button>
    </form>`;

  dashboardEls.content.querySelector("#profile-phone").addEventListener("input", (event) => {
    event.target.value = event.target.value.replace(/[^\d+ ().-]/g, "");
  });
  dashboardEls.content.querySelector("#profile-form").addEventListener("submit", submitProfile);
}

async function submitProfile(event) {
  event.preventDefault();
  const fullName = document.querySelector("#profile-full-name").value.trim();
  const phone = document.querySelector("#profile-phone").value.trim();
  const venueName = document.querySelector("#profile-venue-name").value.trim();
  const locationSelect = document.querySelector("#profile-location-id");
  const newLocationName = document.querySelector("#profile-new-location-name").value.trim();
  const newLocationAddress = document.querySelector("#profile-new-location-address").value.trim();
  const newLocationDescription = document.querySelector("#profile-new-location-description").value.trim();
  let locationId = locationSelect.value || null;

  try {
    if (newLocationName || newLocationAddress || newLocationDescription) {
      if (!newLocationName || !newLocationAddress) {
        showDashboardToast("Для новой локации заполните название и адрес.", true);
        return;
      }

      const createdLocation = await requestJson(dashboardApi.locations, {
        method: "POST",
        body: {
          name: newLocationName,
          address: newLocationAddress,
          description: newLocationDescription || null
        }
      });
      locationId = createdLocation.id;
    }

    await requestJson(dashboardApi.profile, {
      method: "PUT",
      body: {
        fullName,
        phone,
        venueName: venueName || null,
        locationId
      }
    });

    dashboardState.cache.delete("profile");
    dashboardState.cache.delete("locations");
    showDashboardToast("Профиль сохранен.", false);
    await renderProfile(true);
  } catch (error) {
    showDashboardToast(error.message, true);
  }
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

async function renderSpecialistServices(force) {
  dashboardEls.content.innerHTML = `<div class="empty-state">Загрузка...</div>`;
  const [catalogServices, specialistServices] = await Promise.all([
    getCached("catalog-services", dashboardApi.catalogServices, force),
    getCached("specialist-services", dashboardApi.specialistServices, force)
  ]);

  dashboardEls.content.innerHTML = `
    <section class="service-manager">
      <form class="service-editor" id="specialist-service-form">
        <input id="specialist-service-id" type="hidden">
        <div class="section-title">
          <h2>Услуга специалиста</h2>
          <button type="button" class="secondary-button" id="service-form-reset">Сбросить</button>
        </div>
        <div class="two-column service-editor-grid">
          <div class="field-group">
            <label for="specialist-service-select">Категория услуги</label>
            <select id="specialist-service-select">
              <option value="">Выберите из каталога</option>
              ${catalogServices.map((service) => `<option value="${service.id}">${escapeDashboardHtml(service.name)}</option>`).join("")}
            </select>
          </div>
          <div class="field-group">
            <label for="new-catalog-service-name">Новая категория</label>
            <input id="new-catalog-service-name" type="text" maxlength="120" placeholder="Например: Консультация">
          </div>
        </div>
        <div class="field-group">
          <label for="new-catalog-service-description">Описание новой категории</label>
          <input id="new-catalog-service-description" type="text" maxlength="500">
        </div>
        <div class="two-column service-editor-grid">
          <div class="field-group">
            <label for="specialist-service-price">Цена</label>
            <input id="specialist-service-price" type="number" min="1" step="0.01" required>
          </div>
          <div class="field-group">
            <label for="specialist-service-duration">Длительность, мин</label>
            <input id="specialist-service-duration" type="number" min="1" step="1" required>
          </div>
        </div>
        <button class="primary-button" id="specialist-service-submit" type="submit">Добавить услугу</button>
      </form>

      <div class="data-table-wrap">
        <table class="data-table services-table">
          <thead>
            <tr><th>Услуга</th><th>Цена</th><th>Длительность</th><th>Действия</th></tr>
          </thead>
          <tbody>${specialistServices.map(renderSpecialistServiceRow).join("") || `<tr><td colspan="4">Услуги специалиста еще не настроены.</td></tr>`}</tbody>
        </table>
      </div>
    </section>`;

  const form = dashboardEls.content.querySelector("#specialist-service-form");
  form.addEventListener("submit", submitSpecialistService);
  dashboardEls.content.querySelector("#service-form-reset").addEventListener("click", resetSpecialistServiceForm);
  dashboardEls.content.querySelectorAll("[data-service-edit]").forEach((button) => {
    button.addEventListener("click", () => editSpecialistService(button.dataset.serviceEdit));
  });
  dashboardEls.content.querySelectorAll("[data-service-delete]").forEach((button) => {
    button.addEventListener("click", () => deleteSpecialistService(button.dataset.serviceDelete));
  });
}

function renderSpecialistServiceRow(service) {
  return `
    <tr data-specialist-service-row="${service.id}">
      <td>${escapeDashboardHtml(service.serviceName || "Без названия")}</td>
      <td>${formatDashboardMoney(service.price)}</td>
      <td>${service.durationMinutes} мин</td>
      <td>
        <div class="row-actions">
          <button type="button" class="table-action" data-service-edit="${service.id}">Редактировать</button>
          <button type="button" class="table-action" data-service-delete="${service.id}">Удалить</button>
        </div>
      </td>
    </tr>`;
}

async function submitSpecialistService(event) {
  event.preventDefault();
  const serviceIdInput = document.querySelector("#specialist-service-id");
  const catalogSelect = document.querySelector("#specialist-service-select");
  const newNameInput = document.querySelector("#new-catalog-service-name");
  const newDescriptionInput = document.querySelector("#new-catalog-service-description");
  const priceInput = document.querySelector("#specialist-service-price");
  const durationInput = document.querySelector("#specialist-service-duration");
  const specialistServiceId = serviceIdInput.value;
  let serviceId = catalogSelect.value;

  try {
    if (newNameInput.value.trim()) {
      const createdService = await requestJson(dashboardApi.catalogServices, {
        method: "POST",
        body: {
          name: newNameInput.value.trim(),
          description: newDescriptionInput.value.trim() || null
        }
      });
      serviceId = createdService.id;
    }

    if (!serviceId) {
      showDashboardToast("Выберите услугу из каталога или создайте новую категорию.", true);
      return;
    }

    const payload = {
      serviceId,
      price: Number(priceInput.value),
      durationMinutes: Number(durationInput.value)
    };
    const url = specialistServiceId
      ? dashboardApi.specialistService(specialistServiceId)
      : dashboardApi.specialistServices;
    const method = specialistServiceId ? "PUT" : "POST";

    await requestJson(url, { method, body: payload });
    dashboardState.cache.delete("specialist-services");
    dashboardState.cache.delete("catalog-services");
    dashboardState.editingSpecialistServiceId = "";
    await renderSpecialistServices(true);
    showDashboardToast("Услуга сохранена.", false);
  } catch (error) {
    showDashboardToast(error.message, true);
  }
}

async function editSpecialistService(specialistServiceId) {
  const specialistServices = await getCached("specialist-services", dashboardApi.specialistServices, false);
  const service = specialistServices.find((item) => item.id === specialistServiceId);
  if (!service) {
    showDashboardToast("Услуга не найдена.", true);
    return;
  }

  dashboardState.editingSpecialistServiceId = specialistServiceId;
  document.querySelector("#specialist-service-id").value = service.id;
  document.querySelector("#specialist-service-select").value = service.serviceId;
  document.querySelector("#new-catalog-service-name").value = "";
  document.querySelector("#new-catalog-service-description").value = "";
  document.querySelector("#specialist-service-price").value = service.price;
  document.querySelector("#specialist-service-duration").value = service.durationMinutes;
  document.querySelector("#specialist-service-submit").textContent = "Сохранить изменения";
}

async function deleteSpecialistService(specialistServiceId) {
  if (!window.confirm("Удалить услугу специалиста?")) {
    return;
  }

  try {
    await requestJson(dashboardApi.specialistService(specialistServiceId), { method: "DELETE" });
    dashboardState.cache.delete("specialist-services");
    await renderSpecialistServices(true);
    showDashboardToast("Услуга удалена.", false);
  } catch (error) {
    showDashboardToast(error.message, true);
  }
}

function resetSpecialistServiceForm() {
  document.querySelector("#specialist-service-id").value = "";
  document.querySelector("#specialist-service-select").value = "";
  document.querySelector("#new-catalog-service-name").value = "";
  document.querySelector("#new-catalog-service-description").value = "";
  document.querySelector("#specialist-service-price").value = "";
  document.querySelector("#specialist-service-duration").value = "";
  document.querySelector("#specialist-service-submit").textContent = "Добавить услугу";
}

async function renderCalendar(force) {
  dashboardEls.content.innerHTML = `<div class="empty-state">Загрузка...</div>`;
  const range = getCalendarRange();
  const cacheKey = `calendar:${dashboardState.calendarView}:${dashboardState.calendarDate}`;
  const events = await getCached(cacheKey, dashboardApi.calendar(range.from, range.to), force);

  dashboardEls.content.innerHTML = `
    <div class="calendar-toolbar">
      <div class="segmented-control" role="tablist" aria-label="Вид календаря">
        <button type="button" class="segment-button${dashboardState.calendarView === "month" ? " active" : ""}" data-calendar-view="month">Месяц</button>
        <button type="button" class="segment-button${dashboardState.calendarView === "week" ? " active" : ""}" data-calendar-view="week">Неделя</button>
        <button type="button" class="segment-button${dashboardState.calendarView === "day" ? " active" : ""}" data-calendar-view="day">День</button>
      </div>
      <div class="calendar-nav">
        <button type="button" class="secondary-button" data-calendar-shift="-1">Назад</button>
        <input id="calendar-date" type="date" value="${dashboardState.calendarDate}">
        <button type="button" class="secondary-button" data-calendar-shift="1">Вперед</button>
      </div>
    </div>
    <div class="calendar-heading">${escapeDashboardHtml(formatCalendarRange(range))}</div>
    ${renderCalendarBody(events, range)}`;

  dashboardEls.content.querySelectorAll("[data-calendar-view]").forEach((button) => {
    button.addEventListener("click", () => {
      dashboardState.calendarView = button.dataset.calendarView;
      renderCalendar(true);
    });
  });
  dashboardEls.content.querySelectorAll("[data-calendar-shift]").forEach((button) => {
    button.addEventListener("click", () => {
      shiftCalendar(Number(button.dataset.calendarShift));
      renderCalendar(true);
    });
  });
  dashboardEls.content.querySelector("#calendar-date").addEventListener("change", (event) => {
    dashboardState.calendarDate = event.target.value || todayIso();
    renderCalendar(true);
  });
  dashboardEls.content.querySelectorAll("[data-calendar-reschedule]").forEach((button) => {
    button.addEventListener("click", () => openRescheduleDialog(button.dataset.calendarReschedule));
  });
  dashboardEls.content.querySelectorAll("[data-calendar-cancel]").forEach((button) => {
    button.addEventListener("click", () => cancelCalendarBooking(button.dataset.calendarCancel));
  });
}

function renderCalendarBody(events, range) {
  if (dashboardState.calendarView === "day") {
    return `
      <div class="calendar-day-column">
        ${renderCalendarEvents(events.filter((event) => event.date === dashboardState.calendarDate))}
      </div>`;
  }

  const days = dashboardState.calendarView === "week"
    ? daysBetween(range.from, range.to)
    : daysBetween(startOfWeek(range.from), endOfWeek(range.to));
  const className = dashboardState.calendarView === "week" ? "calendar-grid week-view" : "calendar-grid month-view";
  return `
    <div class="${className}">
      ${days.map((date) => {
        const dayEvents = events.filter((event) => event.date === date);
        const isOutsideMonth = dashboardState.calendarView === "month"
          && parseIsoDate(date).getMonth() !== parseIsoDate(dashboardState.calendarDate).getMonth();
        return `
          <section class="calendar-cell${isOutsideMonth ? " muted-day" : ""}">
            <header>${escapeDashboardHtml(formatShortDate(date))}</header>
            ${renderCalendarEvents(dayEvents)}
          </section>`;
      }).join("")}
    </div>`;
}

function renderCalendarEvents(events) {
  return events
    .map((event) => `
      <article class="calendar-event status-border-${event.status}">
        <div>
          <strong>${escapeDashboardHtml(event.startTime.slice(0, 5))}-${escapeDashboardHtml(event.endTime.slice(0, 5))}</strong>
          <span>${escapeDashboardHtml(event.clientName)}</span>
          <small>${escapeDashboardHtml(event.services.map((service) => service.serviceName).join(", ") || "Без услуги")}</small>
        </div>
        <div class="row-actions">
          <button type="button" class="table-action" data-calendar-reschedule="${event.id}">Перенести</button>
          <button type="button" class="table-action" data-calendar-cancel="${event.id}">Удалить</button>
        </div>
      </article>`)
    .join("") || `<div class="calendar-empty">Нет записей</div>`;
}

async function openRescheduleDialog(bookingId) {
  const range = getCalendarRange();
  const events = await getCached(`calendar:${dashboardState.calendarView}:${dashboardState.calendarDate}`, dashboardApi.calendar(range.from, range.to), false);
  const event = events.find((item) => item.id === bookingId);
  if (!event) {
    showDashboardToast("Запись не найдена.", true);
    return;
  }

  dashboardEls.dialogTitle.textContent = "Перенести запись";
  dashboardEls.dialogBody.innerHTML = `
    <div class="two-column">
      <div class="field-group"><label for="reschedule-date">Дата</label><input id="reschedule-date" type="date" value="${event.date}" required></div>
      <div class="field-group"><label for="reschedule-time">Время</label><input id="reschedule-time" type="time" value="${event.startTime.slice(0, 5)}" required></div>
    </div>`;
  dashboardEls.actionForm.onsubmit = async (submitEvent) => {
    submitEvent.preventDefault();
    await submitReschedule(bookingId);
  };
  dashboardEls.dialog.showModal();
}

async function submitReschedule(bookingId) {
  try {
    await requestJson(dashboardApi.reschedule(bookingId), {
      method: "PUT",
      body: {
        date: document.querySelector("#reschedule-date")?.value,
        time: normalizeTime(document.querySelector("#reschedule-time")?.value)
      }
    });
    closeDialog();
    dashboardState.cache.clear();
    await renderCalendar(true);
    showDashboardToast("Запись перенесена.", false);
  } catch (error) {
    showDashboardToast(error.message, true);
  }
}

async function cancelCalendarBooking(bookingId) {
  if (!window.confirm("Удалить запись из календаря?")) {
    return;
  }

  try {
    await requestJson(dashboardApi.cancelCalendar(bookingId), { method: "DELETE" });
    dashboardState.cache.clear();
    await renderCalendar(true);
    showDashboardToast("Запись удалена из календаря.", false);
  } catch (error) {
    showDashboardToast(error.message, true);
  }
}

async function renderKanban(force) {
  dashboardEls.content.innerHTML = `<div class="empty-state">Загрузка...</div>`;
  const board = await getCached(`kanban:${dashboardState.kanbanDate}`, dashboardApi.kanban(dashboardState.kanbanDate), force);
  dashboardEls.content.innerHTML = `
    <div class="table-toolbar kanban-toolbar">
      <input id="kanban-date" type="date" value="${dashboardState.kanbanDate}">
    </div>
    <div class="kanban-board">
      ${board.columns.map(renderKanbanColumn).join("")}
    </div>`;

  dashboardEls.content.querySelector("#kanban-date").addEventListener("change", (event) => {
    dashboardState.kanbanDate = event.target.value || todayIso();
    renderKanban(true);
  });
  bindKanbanDrag();
}

function renderKanbanColumn(column) {
  return `
    <section class="kanban-column" data-kanban-status="${column.status}">
      <header>
        <span>${statusText(column.status)}</span>
        <strong>${column.items.length}</strong>
      </header>
      <div class="kanban-column-body" data-drop-status="${column.status}">
        ${column.items.map(renderKanbanCard).join("") || `<div class="calendar-empty">Нет заявок</div>`}
      </div>
    </section>`;
}

function renderKanbanCard(card) {
  return `
    <article class="kanban-card" draggable="true" data-kanban-card="${card.id}">
      <strong>${escapeDashboardHtml(card.clientName)}</strong>
      <span>${escapeDashboardHtml(card.time.slice(0, 5))} · ${formatDashboardMoney(card.totalPrice)}</span>
      <small>${escapeDashboardHtml(card.servicesSummary || "Без услуги")}</small>
    </article>`;
}

function bindKanbanDrag() {
  dashboardEls.content.querySelectorAll("[data-kanban-card]").forEach((card) => {
    card.addEventListener("dragstart", (event) => {
      event.dataTransfer.setData("text/plain", card.dataset.kanbanCard);
      event.dataTransfer.effectAllowed = "move";
    });
  });

  dashboardEls.content.querySelectorAll("[data-drop-status]").forEach((column) => {
    column.addEventListener("dragover", (event) => {
      event.preventDefault();
      column.classList.add("drag-over");
    });
    column.addEventListener("dragleave", () => column.classList.remove("drag-over"));
    column.addEventListener("drop", async (event) => {
      event.preventDefault();
      column.classList.remove("drag-over");
      const bookingId = event.dataTransfer.getData("text/plain");
      await moveKanbanCard(bookingId, Number(column.dataset.dropStatus));
    });
  });
}

async function moveKanbanCard(bookingId, status) {
  try {
    await requestJson(dashboardApi.kanbanMove(bookingId), {
      method: "PUT",
      body: { status }
    });
    dashboardState.cache.clear();
    await renderKanban(true);
    showDashboardToast("Статус заявки обновлен.", false);
  } catch (error) {
    showDashboardToast(error.message, true);
  }
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

function getCalendarRange() {
  const anchor = parseIsoDate(dashboardState.calendarDate);
  if (dashboardState.calendarView === "day") {
    return { from: dashboardState.calendarDate, to: dashboardState.calendarDate };
  }

  if (dashboardState.calendarView === "week") {
    return {
      from: startOfWeek(formatIsoDate(anchor)),
      to: endOfWeek(formatIsoDate(anchor))
    };
  }

  return {
    from: formatIsoDate(new Date(anchor.getFullYear(), anchor.getMonth(), 1)),
    to: formatIsoDate(new Date(anchor.getFullYear(), anchor.getMonth() + 1, 0))
  };
}

function shiftCalendar(direction) {
  const anchor = parseIsoDate(dashboardState.calendarDate);
  if (dashboardState.calendarView === "day") {
    anchor.setDate(anchor.getDate() + direction);
  } else if (dashboardState.calendarView === "week") {
    anchor.setDate(anchor.getDate() + direction * 7);
  } else {
    anchor.setMonth(anchor.getMonth() + direction);
  }

  dashboardState.calendarDate = formatIsoDate(anchor);
}

function daysBetween(from, to) {
  const dates = [];
  const current = parseIsoDate(from);
  const end = parseIsoDate(to);
  while (current <= end) {
    dates.push(formatIsoDate(current));
    current.setDate(current.getDate() + 1);
  }

  return dates;
}

function startOfWeek(isoDate) {
  const date = parseIsoDate(isoDate);
  const day = date.getDay() || 7;
  date.setDate(date.getDate() - day + 1);
  return formatIsoDate(date);
}

function endOfWeek(isoDate) {
  const date = parseIsoDate(startOfWeek(isoDate));
  date.setDate(date.getDate() + 6);
  return formatIsoDate(date);
}

function parseIsoDate(value) {
  return new Date(`${value}T00:00:00`);
}

function formatIsoDate(date) {
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

function todayIso() {
  return formatIsoDate(new Date());
}

function formatShortDate(value) {
  return new Intl.DateTimeFormat("ru-RU", { weekday: "short", day: "2-digit", month: "2-digit" })
    .format(parseIsoDate(value));
}

function formatCalendarRange(range) {
  if (range.from === range.to) {
    return formatShortDate(range.from);
  }

  return `${formatShortDate(range.from)} - ${formatShortDate(range.to)}`;
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
