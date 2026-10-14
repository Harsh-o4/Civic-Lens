const API_BASE = window.CIVIC_API_BASE ?? "http://localhost:5080";
const TOKEN_KEY = "civic-report-token";
// Temporary testing mode: authentication is disabled until the backend flow is restored.
const AUTHENTICATION_ENABLED = false;
const map = L.map("report-map").setView([20.5937, 78.9629], 5);
L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", { attribution: "&copy; OpenStreetMap contributors" }).addTo(map);
let marker;
let selectedLocation;
let selectedMediaFile;
let selectedMediaPreviewUrl;
let registering = false;

const $ = (selector) => document.querySelector(selector);
const token = () => localStorage.getItem(TOKEN_KEY);
const currentRole = () => {
  if (!token()) return null;
  try {
    const payload = JSON.parse(atob(token().split(".")[1].replaceAll("-", "+").replaceAll("_", "/")));
    return payload.role ?? payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  } catch { return null; }
};
const api = async (path, options = {}) => {
  const headers = { "Content-Type": "application/json", ...(options.headers ?? {}) };
  if (token()) headers.Authorization = `Bearer ${token()}`;
  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, { ...options, headers });
  } catch {
    throw new Error(`Cannot connect to the API at ${API_BASE}. Start the backend with "dotnet run --project backend".`);
  }
  if (!response.ok) {
    let detail = `Request failed (${response.status})`;
    try { detail = (await response.json()).detail ?? detail; } catch { /* non-JSON error */ }
    throw new Error(detail);
  }
  return response.status === 204 ? null : response.json();
};

map.on("click", ({ latlng }) => {
  selectedLocation = { latitude: Number(latlng.lat.toFixed(6)), longitude: Number(latlng.lng.toFixed(6)) };
  if (marker) marker.setLatLng(latlng);
  else marker = L.marker(latlng).addTo(map);
  $("#coordinates").textContent = `${selectedLocation.latitude}, ${selectedLocation.longitude}`;
});

$("#media-file").addEventListener("change", (event) => {
  const [file] = event.target.files;
  clearMediaPreview();
  if (!file) return;
  if (!file.type.startsWith("image/")) {
    event.target.value = "";
    return showMessage("#form-message", "Please choose a JPG, PNG, or WebP image.", true);
  }
  if (file.size > 5 * 1024 * 1024) {
    event.target.value = "";
    return showMessage("#form-message", "Images must be smaller than 5 MB.", true);
  }
  selectedMediaFile = file;
  selectedMediaPreviewUrl = URL.createObjectURL(file);
  const previewImage = document.createElement("img");
  previewImage.alt = "Selected issue photo preview";
  previewImage.onload = () => {
    $("#preview-image-container").replaceChildren(previewImage);
    $("#preview-label").textContent = `${file.name} (${formatFileSize(file.size)})`;
    $("#media-preview").hidden = false;
  };
  previewImage.onerror = () => {
    clearMediaPreview();
    $("#media-file").value = "";
    showMessage("#form-message", "The selected image could not be previewed.", true);
  };
  previewImage.src = selectedMediaPreviewUrl;
  showMessage("#form-message", "");
});

$("#remove-media").addEventListener("click", () => {
  $("#media-file").value = "";
  clearMediaPreview();
});

$("#issue-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  // if (AUTHENTICATION_ENABLED && !token()) return openAuth("Sign in to submit an issue.");
  if (!selectedLocation) return showMessage("#form-message", "Choose a location on the map.", true);
  const form = new FormData(event.currentTarget);
  const body = { title: form.get("title"), description: form.get("description"), categoryId: form.get("categoryId"), ...selectedLocation };
  // The current API accepts media metadata only. Binary storage/upload will be added
  // with the media endpoint; the selected file is still validated and previewed here.
  if (selectedMediaFile) {
    body.media = [{ mediaUrl: `https://pending-upload.local/${encodeURIComponent(selectedMediaFile.name)}`, contentType: selectedMediaFile.type }];
  }
  try {
    await api("/api/v1/issues", { method: "POST", body: JSON.stringify(body) });
    event.currentTarget.reset(); selectedLocation = undefined; clearMediaPreview();
    if (marker) { map.removeLayer(marker); marker = undefined; }
    $("#coordinates").textContent = "Click the map to place a pin.";
    showMessage("#form-message", "Issue submitted. Thank you for helping your community.");
    loadIssues();
  } catch (error) { showMessage("#form-message", error.message, true); }
});

async function loadIssues() {
  const params = new URLSearchParams();
  for (const id of ["status-filter", "min-latitude", "max-latitude"]) {
    const value = $(`#${id}`).value;
    if (value) params.set(id.replace("-filter", "").replaceAll("-", ""), value);
  }
  if ($("#min-latitude").value) params.set("minLatitude", $("#min-latitude").value);
  if ($("#max-latitude").value) params.set("maxLatitude", $("#max-latitude").value);
  try {
    const issues = await api(`/api/v1/issues?${params}`);
    $("#issue-count").textContent = issues.length;
    $("#issue-list").innerHTML = issues.length ? issues.map(issue => `
      <article class="issue-row"><div><h3>${escapeHtml(issue.title)}</h3>
      <p>${escapeHtml(issue.description.slice(0, 140))}${issue.description.length > 140 ? "..." : ""}</p>
      <p>${issue.latitude.toFixed(4)}, ${issue.longitude.toFixed(4)}</p></div>
      <div class="issue-actions"><span class="status">${escapeHtml(issue.status)}</span>
      ${currentRole() === "Admin" ? `<select data-status-id="${issue.id}" aria-label="New status">
      ${["Submitted", "InReview", "Assigned", "InProgress", "Resolved", "Closed"].map(status => `<option ${status === issue.status ? "selected" : ""}>${status}</option>`).join("")}</select>
      <button class="button button-dark update-status" data-id="${issue.id}" type="button">Update</button>
      <input data-tech-id="${issue.id}" placeholder="FieldTech UUID" aria-label="Field technician UUID">
      <button class="button button-outline assign-issue" data-id="${issue.id}" type="button">Assign</button>` : ""}</div></article>`).join("") : '<p class="empty-state">No issues match these filters.</p>';
    document.querySelectorAll(".update-status").forEach(button => button.addEventListener("click", () => updateStatus(button.dataset.id)));
    document.querySelectorAll(".assign-issue").forEach(button => button.addEventListener("click", () => assignIssue(button.dataset.id)));
  } catch (error) { $("#issue-list").innerHTML = `<p class="empty-state">${escapeHtml(error.message)}</p>`; }
}

async function updateStatus(id) {
  const status = document.querySelector(`[data-status-id="${id}"]`).value;
  try { await api(`/api/v1/issues/${id}/status`, { method: "PATCH", body: JSON.stringify({ status }) }); showToast("Status updated."); loadIssues(); }
  catch (error) { showToast(error.message); }
}
async function assignIssue(id) {
  const fieldTechnicianId = document.querySelector(`[data-tech-id="${id}"]`).value;
  if (!fieldTechnicianId) return showToast("Enter a FieldTech UUID.");
  try { await api(`/api/v1/issues/${id}/assign`, { method: "POST", body: JSON.stringify({ fieldTechnicianId }) }); showToast("Issue assigned."); loadIssues(); }
  catch (error) { showToast(error.message); }
}

function escapeHtml(value) { return String(value).replace(/[&<>"']/g, (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#039;" })[char]); }
function showMessage(selector, message, error = false) { const element = $(selector); element.textContent = message; element.className = `form-message ${error ? "error" : "success"}`; }
function showToast(message) { const toast = $("#toast"); toast.textContent = message; toast.classList.add("visible"); setTimeout(() => toast.classList.remove("visible"), 3500); }
function clearMediaPreview() {
  if (selectedMediaPreviewUrl) URL.revokeObjectURL(selectedMediaPreviewUrl);
  selectedMediaFile = undefined;
  selectedMediaPreviewUrl = undefined;
  $("#preview-image-container").replaceChildren();
  $("#preview-label").textContent = "";
  $("#media-preview").hidden = true;
}
function formatFileSize(bytes) {
  return bytes < 1024 * 1024 ? `${Math.ceil(bytes / 1024)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}
function openAuth(message = "") { $("#auth-dialog").showModal(); if (message) showMessage("#auth-message", message); }
function updateAuthButton() { $("#auth-button").textContent = token() ? "Sign out" : "Sign in"; }

$("#auth-button").addEventListener("click", () => token() ? (localStorage.removeItem(TOKEN_KEY), updateAuthButton(), showToast("Signed out.")) : openAuth());
$("#close-auth").addEventListener("click", () => $("#auth-dialog").close());
$("#toggle-auth").addEventListener("click", () => {
  registering = !registering;
  $("#auth-title").textContent = registering ? "Create account" : "Sign in";
  $("#auth-submit").textContent = registering ? "Register" : "Sign in";
  $("#name-field").hidden = !registering;
  $("#toggle-auth").textContent = registering ? "Already have an account? Sign in" : "Need an account? Register";
});
$("#auth-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const form = new FormData(event.currentTarget);
  const body = { email: form.get("email"), password: form.get("password") };
  if (registering) body.fullName = form.get("fullName");
  try {
    const result = await api(registering ? "/api/v1/auth/register" : "/api/v1/auth/login", { method: "POST", body: JSON.stringify(body) });
    localStorage.setItem(TOKEN_KEY, result.token); $("#auth-dialog").close(); updateAuthButton(); showToast("You are signed in."); loadIssues();
  } catch (error) { showMessage("#auth-message", error.message, true); }
});
$("#refresh-button").addEventListener("click", loadIssues);
$("#filter-button").addEventListener("click", loadIssues);
updateAuthButton();
loadIssues();
