const api = {
  services: "/api/v1/services",
  locations: (serviceId) => `/api/v1/locations?serviceId=${encodeURIComponent(serviceId)}`,
  specialists: (locationId, serviceId) =>
    `/api/v1/specialists?locationId=${encodeURIComponent(locationId)}&serviceId=${encodeURIComponent(serviceId)}`,
  specialistServices: (specialistId) => `/api/v1/specialists/${encodeURIComponent(specialistId)}/services`,
  slots: (specialistId, date, duration) =>
    `/api/v1/specialists/${encodeURIComponent(specialistId)}/slots?date=${encodeURIComponent(date)}&durationMinutes=${duration}`,
  bookings: "/api/v1/bookings"
};

const state = {
  step: 0,
  services: [],
  locations: [],
  specialists: [],
  specialistServices: [],
  slots: [],
  selectedServiceCategoryId: "",
  selectedLocationId: "",
  selectedSpecialistId: "",
  selectedServiceIds: new Set(),
  selectedTime: "",
  loading: false
};

const els = {
  tabs: [...document.querySelectorAll(".step-tab")],
  cards: [...document.querySelectorAll(".step-card")],
  serviceSearch: document.querySelector("#service-search"),
  servicesList: document.querySelector("#services-list"),
  locationsList: document.querySelector("#locations-list"),
  specialistsList: document.querySelector("#specialists-list"),
  specialistServicesList: document.querySelector("#specialist-services-list"),
  servicesTotal: document.querySelector("#services-total"),
  summaryPill: document.querySelector("#summary-pill"),
  clientName: document.querySelector("#client-name"),
  clientPhone: document.querySelector("#client-phone"),
  clientNameError: document.querySelector("#client-name-error"),
  clientPhoneError: document.querySelector("#client-phone-error"),
  requestedDate: document.querySelector("#requested-date"),
  requestedTime: document.querySelector("#requested-time"),
  slotsList: document.querySelector("#slots-list"),
  message: document.querySelector("#message"),
  messageCounter: document.querySelector("#message-counter"),
  backButton: document.querySelector("#back-button"),
  nextButton: document.querySelector("#next-button"),
  submitButton: document.querySelector("#submit-button"),
  form: document.querySelector("#booking-form"),
  resultBanner: document.querySelector("#result-banner")
};

document.addEventListener("DOMContentLoaded", init);

async function init() {
  const today = new Date().toISOString().slice(0, 10);
  els.requestedDate.min = today;
  els.requestedDate.value = today;

  bindEvents();
  setStep(0);
  await loadServices();
  validateForm();
}

function bindEvents() {
  els.tabs.forEach((tab) => {
    tab.addEventListener("click", () => setStep(Number(tab.dataset.stepTarget)));
  });

  els.serviceSearch.addEventListener("input", renderServices);
  els.clientName.addEventListener("input", validateForm);
  els.clientPhone.addEventListener("input", () => {
    els.clientPhone.value = els.clientPhone.value.replace(/[^\d+ ().-]/g, "");
    validateForm();
  });
  els.requestedDate.addEventListener("change", loadSlots);
  els.requestedTime.addEventListener("change", () => {
    state.selectedTime = els.requestedTime.value;
    renderSlots();
    validateForm();
  });
  els.message.addEventListener("input", () => {
    els.messageCounter.textContent = `${els.message.value.length}/500`;
    validateForm();
  });
  els.backButton.addEventListener("click", () => setStep(Math.max(0, state.step - 1)));
  els.nextButton.addEventListener("click", () => setStep(Math.min(6, state.step + 1)));
  els.form.addEventListener("submit", submitBooking);
}

async function loadServices() {
  setLoading(els.servicesList);
  try {
    state.services = await getJson(api.services);
    renderServices();
  } catch {
    setError(els.servicesList, "Не удалось загрузить услуги.");
  }
}

async function selectServiceCategory(serviceId) {
  state.selectedServiceCategoryId = serviceId;
  state.selectedLocationId = "";
  state.selectedSpecialistId = "";
  state.selectedServiceIds.clear();
  state.locations = [];
  state.specialists = [];
  state.specialistServices = [];
  state.slots = [];
  renderServices();
  renderSpecialistServices();
  updateTotals();
  validateForm();
  await loadLocations();
  setStep(1);
}

async function loadLocations() {
  if (!state.selectedServiceCategoryId) {
    renderLocations();
    return;
  }

  setLoading(els.locationsList);
  try {
    state.locations = await getJson(api.locations(state.selectedServiceCategoryId));
    renderLocations();
  } catch {
    setError(els.locationsList, "Не удалось загрузить локации.");
  }
}

async function selectLocation(locationId) {
  state.selectedLocationId = locationId;
  state.selectedSpecialistId = "";
  state.selectedServiceIds.clear();
  state.specialists = [];
  state.specialistServices = [];
  state.slots = [];
  renderLocations();
  renderSpecialistServices();
  updateTotals();
  validateForm();
  await loadSpecialists();
  setStep(2);
}

async function loadSpecialists() {
  if (!state.selectedLocationId || !state.selectedServiceCategoryId) {
    renderSpecialists();
    return;
  }

  setLoading(els.specialistsList);
  try {
    state.specialists = await getJson(api.specialists(state.selectedLocationId, state.selectedServiceCategoryId));
    renderSpecialists();
  } catch {
    setError(els.specialistsList, "Не удалось загрузить специалистов.");
  }
}

async function selectSpecialist(specialistId) {
  state.selectedSpecialistId = specialistId;
  state.selectedServiceIds.clear();
  state.specialistServices = [];
  state.slots = [];
  renderSpecialists();
  updateTotals();
  validateForm();
  await loadSpecialistServices();
  setStep(3);
}

async function loadSpecialistServices() {
  if (!state.selectedSpecialistId) {
    renderSpecialistServices();
    return;
  }

  setLoading(els.specialistServicesList);
  try {
    state.specialistServices = await getJson(api.specialistServices(state.selectedSpecialistId));
    renderSpecialistServices();
  } catch {
    setError(els.specialistServicesList, "Не удалось загрузить услуги специалиста.");
  }
}

function toggleSpecialistService(serviceId) {
  if (state.selectedServiceIds.has(serviceId)) {
    state.selectedServiceIds.delete(serviceId);
  } else {
    state.selectedServiceIds.add(serviceId);
  }

  renderSpecialistServices();
  updateTotals();
  loadSlots();
  validateForm();
}

async function loadSlots() {
  const total = getSelectedTotals();
  state.selectedTime = "";
  els.requestedTime.value = "";

  if (!state.selectedSpecialistId || !els.requestedDate.value) {
    renderSlots();
    validateForm();
    return;
  }

  setLoading(els.slotsList);
  try {
    const duration = Math.max(total.duration, 30);
    state.slots = await getJson(api.slots(state.selectedSpecialistId, els.requestedDate.value, duration));
    renderSlots();
  } catch {
    setError(els.slotsList, "Не удалось загрузить слоты.");
  }

  validateForm();
}

function renderServices() {
  const query = els.serviceSearch.value.trim().toLowerCase();
  const services = state.services.filter((service) => service.name.toLowerCase().includes(query));
  renderOptionList(els.servicesList, services, state.selectedServiceCategoryId, selectServiceCategory, (service) => ({
    title: service.name,
    meta: service.description || "Категория услуги"
  }));
}

function renderLocations() {
  renderOptionList(els.locationsList, state.locations, state.selectedLocationId, selectLocation, (location) => ({
    title: location.name,
    meta: [location.address, location.description].filter(Boolean).join(" · ")
  }));
}

function renderSpecialists() {
  els.specialistsList.innerHTML = "";
  if (state.specialists.length === 0) {
    els.specialistsList.innerHTML = `<div class="empty-state">Специалисты не найдены.</div>`;
    return;
  }

  for (const specialist of state.specialists) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `specialist-card${specialist.id === state.selectedSpecialistId ? " selected" : ""}`;
    button.addEventListener("click", () => selectSpecialist(specialist.id));
    const initials = specialist.fullName
      .split(" ")
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join("")
      .toUpperCase();
    const avatar = specialist.avatarUrl
      ? `<img src="${escapeHtml(specialist.avatarUrl)}" alt="">`
      : escapeHtml(initials || "SB");
    button.innerHTML = `
      <div class="avatar-row">
        <span class="avatar">${avatar}</span>
        <span>
          <span class="item-title">${escapeHtml(specialist.fullName)}</span>
          <span class="item-meta">${escapeHtml(specialist.venueName || specialist.locationName || "Специалист")}</span>
        </span>
      </div>`;
    els.specialistsList.append(button);
  }
}

function renderSpecialistServices() {
  els.specialistServicesList.innerHTML = "";
  if (state.specialistServices.length === 0) {
    els.specialistServicesList.innerHTML = `<div class="empty-state">Выберите специалиста.</div>`;
    return;
  }

  for (const service of state.specialistServices) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = `toggle-button${state.selectedServiceIds.has(service.serviceId) ? " selected" : ""}`;
    button.addEventListener("click", () => toggleSpecialistService(service.serviceId));
    button.innerHTML = `
      <span class="item-title">${escapeHtml(service.serviceName)}</span>
      <span class="item-meta">${formatMoney(service.price)} · ${service.durationMinutes} мин</span>`;
    els.specialistServicesList.append(button);
  }
}

function renderSlots() {
  els.requestedTime.innerHTML = `<option value="">Выберите слот</option>`;
  els.slotsList.innerHTML = "";

  if (state.slots.length === 0) {
    els.slotsList.innerHTML = `<div class="empty-state">На выбранную дату нет доступных слотов.</div>`;
    return;
  }

  for (const slot of state.slots) {
    const time = slot.time.slice(0, 5);
    const option = document.createElement("option");
    option.value = time;
    option.textContent = time;
    option.selected = state.selectedTime === time;
    els.requestedTime.append(option);

    const button = document.createElement("button");
    button.type = "button";
    button.className = `slot-button${state.selectedTime === time ? " selected" : ""}`;
    button.textContent = time;
    button.addEventListener("click", () => {
      state.selectedTime = time;
      els.requestedTime.value = time;
      renderSlots();
      validateForm();
    });
    els.slotsList.append(button);
  }
}

function renderOptionList(container, items, selectedId, onSelect, map) {
  container.innerHTML = "";
  if (items.length === 0) {
    container.innerHTML = `<div class="empty-state">Нет доступных вариантов.</div>`;
    return;
  }

  for (const item of items) {
    const view = map(item);
    const button = document.createElement("button");
    button.type = "button";
    button.className = `option-button${item.id === selectedId ? " selected" : ""}`;
    button.addEventListener("click", () => onSelect(item.id));
    button.innerHTML = `
      <span class="item-title">${escapeHtml(view.title)}</span>
      <span class="item-meta">${escapeHtml(view.meta || "")}</span>`;
    container.append(button);
  }
}

async function submitBooking(event) {
  event.preventDefault();
  validateForm();
  if (els.submitButton.disabled || state.loading) {
    return;
  }

  const payload = {
    clientName: els.clientName.value.trim(),
    clientPhone: els.clientPhone.value.trim(),
    specialistId: state.selectedSpecialistId,
    serviceIds: [...state.selectedServiceIds],
    requestedDate: els.requestedDate.value,
    requestedTime: normalizeBookingTime(state.selectedTime),
    message: els.message.value.trim() || null
  };

  state.loading = true;
  els.submitButton.textContent = "Отправка...";
  showResult("", false);

  try {
    const response = await fetch(api.bookings, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });
    const data = await response.json().catch(() => null);

    if (!response.ok) {
      throw new Error(getErrorMessage(data) || "Заявка не отправлена.");
    }

    showResult(`Заявка создана. Номер: ${data.id}`, false);
    els.form.reset();
    state.selectedServiceIds.clear();
    state.selectedServiceCategoryId = "";
    state.selectedLocationId = "";
    state.selectedSpecialistId = "";
    state.selectedTime = "";
    state.locations = [];
    state.specialists = [];
    state.specialistServices = [];
    state.slots = [];
    const today = new Date().toISOString().slice(0, 10);
    els.requestedDate.value = today;
    els.requestedDate.min = today;
    els.messageCounter.textContent = "0/500";
    renderServices();
    renderLocations();
    renderSpecialists();
    renderSpecialistServices();
    renderSlots();
    updateTotals();
    setStep(0);
  } catch (error) {
    showResult(error.message, true);
  } finally {
    state.loading = false;
    els.submitButton.textContent = "Отправить заявку";
    validateForm();
  }
}

function validateForm() {
  const nameValid = /^[\p{L} ]{2,100}$/u.test(els.clientName.value.trim());
  const phoneValid = /^\+?[0-9][0-9\s().-]{6,30}$/.test(els.clientPhone.value.trim());
  const hasServicesOrMessage = state.selectedServiceIds.size > 0 || els.message.value.trim().length > 0;
  const coreValid = state.selectedServiceCategoryId
    && state.selectedLocationId
    && state.selectedSpecialistId
    && els.requestedDate.value
    && state.selectedTime;

  els.clientNameError.textContent = els.clientName.value && !nameValid ? "Только буквы и пробелы, минимум 2 символа." : "";
  els.clientPhoneError.textContent = els.clientPhone.value && !phoneValid ? "Проверьте формат телефона." : "";
  els.submitButton.disabled = !(nameValid && phoneValid && hasServicesOrMessage && coreValid) || state.loading;

  updateTotals();
}

function updateTotals() {
  const total = getSelectedTotals();
  const text = `${formatMoney(total.price)} · ${total.duration} мин`;
  els.servicesTotal.textContent = text;
  els.summaryPill.textContent = text;
}

function getSelectedTotals() {
  return state.specialistServices
    .filter((service) => state.selectedServiceIds.has(service.serviceId))
    .reduce((total, service) => ({
      price: total.price + service.price,
      duration: total.duration + service.durationMinutes
    }), { price: 0, duration: 0 });
}

function setStep(step) {
  state.step = step;
  els.tabs.forEach((tab, index) => tab.classList.toggle("active", index === step));
  els.cards.forEach((card, index) => card.classList.toggle("active", index === step));
  els.backButton.disabled = step === 0;
  els.nextButton.style.display = step === 6 ? "none" : "";
  validateForm();
}

async function getJson(url) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
  return response.json();
}

function setLoading(container) {
  container.innerHTML = `<div class="empty-state">Загрузка...</div>`;
}

function setError(container, message) {
  container.innerHTML = `<div class="empty-state">${escapeHtml(message)}</div>`;
}

function showResult(message, isError) {
  els.resultBanner.textContent = message;
  els.resultBanner.classList.toggle("visible", Boolean(message));
  els.resultBanner.classList.toggle("error", isError);
}

function formatMoney(value) {
  return new Intl.NumberFormat("ru-RU", {
    style: "currency",
    currency: "RUB",
    maximumFractionDigits: 0
  }).format(value);
}

function normalizeBookingTime(value) {
  return value && value.length === 5 ? `${value}:00` : value;
}

function getErrorMessage(data) {
  if (data?.message) {
    return data.message;
  }

  const firstError = data?.errors && Object.values(data.errors).flat()[0];
  return firstError || data?.title || "";
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
