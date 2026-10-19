const money = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" });
let accounts = [];
let selectedAccountId = null;

async function api(path, options = {}) {
  const response = await fetch(path, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    const details = problem.errors ? Object.values(problem.errors).flat().join(" ") : problem.detail;
    throw new Error(details || problem.title || response.statusText);
  }
  return response.json();
}

function toast(message, isError = false) {
  const el = document.getElementById("toast");
  el.textContent = message;
  el.className = isError ? "error" : "";
  el.hidden = false;
  clearTimeout(el._timer);
  el._timer = setTimeout(() => (el.hidden = true), 3500);
}

function cell(text, className) {
  const td = document.createElement("td");
  td.textContent = text;
  if (className) td.className = className;
  return td;
}

async function loadAccounts() {
  accounts = await api("/api/accounts");

  const tbody = document.querySelector("#accounts tbody");
  tbody.replaceChildren(...accounts.map((a) => {
    const tr = document.createElement("tr");
    tr.dataset.id = a.id;
    tr.classList.toggle("selected", a.id === selectedAccountId);
    tr.append(cell(a.accountNumber), cell(a.ownerName), cell(money.format(a.balance), "num"));
    tr.addEventListener("click", () => selectAccount(a.id));
    return tr;
  }));

  document.querySelectorAll(".account-select").forEach((select) => {
    const previous = select.value;
    select.replaceChildren(...accounts.map((a) => new Option(`${a.accountNumber} — ${a.ownerName}`, a.id)));
    if (accounts.some((a) => a.id === previous)) select.value = previous;
  });
  const to = document.querySelector("[name=toAccountId]");
  if (to.selectedIndex === 0 && accounts.length > 1) to.selectedIndex = 1;

  if (selectedAccountId) await loadTransactions();
}

async function selectAccount(id) {
  selectedAccountId = id;
  document.querySelectorAll("#accounts tbody tr").forEach((tr) => tr.classList.toggle("selected", tr.dataset.id === id));
  await loadTransactions();
}

async function loadTransactions() {
  const account = accounts.find((a) => a.id === selectedAccountId);
  const transactions = await api(`/api/accounts/${selectedAccountId}/transactions`);

  document.getElementById("tx-account").textContent = `— ${account.accountNumber} (${account.ownerName})`;
  document.getElementById("tx-hint").hidden = true;
  document.getElementById("transactions").hidden = false;

  document.querySelector("#transactions tbody").replaceChildren(...transactions.map((t) => {
    const isCredit = t.type === "Deposit" || t.type === "TransferIn";
    const tr = document.createElement("tr");
    tr.append(
      cell(new Date(t.timestampUtc.endsWith("Z") ? t.timestampUtc : t.timestampUtc + "Z").toLocaleString()),
      cell(t.type),
      cell(t.description ?? ""),
      cell(`${isCredit ? "+" : "−"}${money.format(t.amount)}`, `num ${isCredit ? "credit" : "debit"}`),
      cell(money.format(t.balanceAfter), "num"),
    );
    return tr;
  }));
}

function formValues(form) {
  return Object.fromEntries(new FormData(form).entries());
}

function handle(formId, action) {
  document.getElementById(formId).addEventListener("submit", async (event) => {
    event.preventDefault();
    try {
      const message = await action(event.target, event.submitter);
      toast(message);
      await loadAccounts();
    } catch (err) {
      toast(err.message, true);
    }
  });
}

handle("open-form", async (form) => {
  const { ownerName, initialDeposit } = formValues(form);
  const account = await api("/api/accounts", {
    method: "POST",
    body: JSON.stringify({ ownerName, initialDeposit: Number(initialDeposit || 0) }),
  });
  form.reset();
  return `Opened account ${account.accountNumber} for ${account.ownerName}`;
});

handle("money-form", async (form, submitter) => {
  const { accountId, amount, description } = formValues(form);
  const action = submitter?.value ?? "deposit";
  await api(`/api/accounts/${accountId}/${action}`, {
    method: "POST",
    body: JSON.stringify({ amount: Number(amount), description: description || null }),
  });
  form.amount.value = "";
  form.description.value = "";
  return `${action === "deposit" ? "Deposited" : "Withdrew"} ${money.format(amount)}`;
});

handle("transfer-form", async (form) => {
  const { fromAccountId, toAccountId, amount, description } = formValues(form);
  await api("/api/transfers", {
    method: "POST",
    body: JSON.stringify({ fromAccountId, toAccountId, amount: Number(amount), description: description || null }),
  });
  form.amount.value = "";
  form.description.value = "";
  return `Transferred ${money.format(amount)}`;
});

loadAccounts().catch((err) => toast(err.message, true));
