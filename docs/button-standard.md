# ERP Enterprise Button Standard: Icon + One-Word Label

## 1. Mandatory Rule

Every standard action button across the ERP application **must** follow the strict structure:

**Icon + One-Word Label**

```html
<button type="button" class="erp-btn erp-btn-primary" onclick="...">
  <i data-lucide="plus" class="w-3.5 h-3.5"></i>
  <span>Add</span>
</button>
```

### Core Requirements

1. **Exactly One Icon**: Every standard action button must include a single Lucide icon (`<i data-lucide="..."></i>`) that accurately represents the operation. Never use emojis in button labels.
2. **Exactly One Visible Word**: The visible text label inside `<span>...</span>` must be a single concise, action-oriented word (`Add`, `Edit`, `Save`, `Cancel`, `Back`, `Delete`, `Post`, `Refresh`, `Reset`, `Export`, `Print`, etc.). Multi-word phrases such as `Create New Record`, `Save Changes`, `Go Back`, or `Generate Report` are prohibited.
3. **Consistent Alignment & Spacing**: Icons and labels are horizontally aligned (`inline-flex items-center gap-1.5` for default buttons, `gap-1` for compact table/sub-toolbar buttons).
4. **Dynamic & Loading States**: When a button transitions to a loading or busy state (e.g., via `setErpButtonLoading(btn, true, 'save')`), its temporary label must also remain a single word (`Saving`, `Posting`, `Submitting`, `Verifying`).
5. **Contextual Disambiguation**: Where additional business context is helpful (e.g., `title="Add Customer"` or `title="Open Dedicated Audit Studio"`), provide it via the `title` tooltip and/or `aria-label` attribute rather than lengthening the visible button text.

---

## 2. Standardized Action Label & Icon Dictionary

The canonical action dictionary is defined in `window.ERP_BUTTON_STANDARDS` inside `src/ERP.Web.Api/wwwroot/js/ERP-core.js`. Always use the standardized label and icon for equivalent actions across all modules:

| Action Key | Standard One-Word Label | Loading Label | Lucide Icon (`data-lucide`) | Semantic Variant |
|---|---|---|---|---|
| `add` | `Add` | `Adding` | `plus` | `primary` |
| `edit` | `Edit` | `Opening` | `pencil` | `edit` |
| `save` | `Save` | `Saving` | `check` | `success` |
| `cancel` | `Cancel` | `Canceling` | `x` | `cancel` |
| `back` | `Back` | `Leaving` | `arrow-left` | `back` |
| `delete` | `Delete` | `Deleting` | `trash-2` | `delete` |
| `discard` | `Discard` | `Discarding` | `trash-2` | `danger` |
| `search` | `Search` | `Searching` | `search` | `secondary` |
| `filter` | `Filter` | `Filtering` | `filter` | `secondary` |
| `reset` | `Reset` | `Resetting` | `rotate-ccw` | `secondary` |
| `refresh` | `Refresh` | `Refreshing` | `refresh-cw` | `secondary` |
| `view` | `View` | `Opening` | `eye` | `secondary` |
| `details` | `Details` | `Opening` | `file-search` | `secondary` |
| `approve` | `Approve` | `Approving` | `check-circle-2` | `success` |
| `reject` | `Reject` | `Rejecting` | `x-circle` | `danger` |
| `submit` | `Submit` | `Submitting` | `send` | `primary` |
| `confirm` | `Confirm` | `Confirming` | `check` | `primary` |
| `print` | `Print` | `Printing` | `printer` | `secondary` |
| `export` | `Export` | `Exporting` | `download` | `secondary` |
| `import` | `Import` | `Importing` | `upload` | `secondary` |
| `download` | `Download` | `Downloading` | `file-down` | `secondary` |
| `upload` | `Upload` | `Uploading` | `upload` | `secondary` |
| `post` | `Post` | `Posting` | `check-check` | `primary` |
| `reverse` | `Reverse` | `Reversing` | `undo-2` | `warning` |
| `generate` | `Generate` | `Generating` | `sparkles` | `primary` |
| `close` | `Close` | `Closing` | `x` | `secondary` |
| `settings` | `Settings` | `Opening` | `settings` | `secondary` |
| `audit` | `Audit` | `Opening` | `history` | `audit` |
| `restore` | `Restore` | `Restoring` | `rotate-ccw` | `restore` |
| `verify` | `Verify` | `Verifying` | `shield-check` | `secondary` |
| `balance` | `Balance` | `Balancing` | `wand-2` | `secondary` |
| `allocate` | `Allocate` | `Allocating` | `wand-2` | `secondary` |
| `notify` | `Notify` | `Notifying` | `mail` | `secondary` |
| `pay` | `Pay` | `Paying` | `banknote` | `primary` |
| `transfer` | `Transfer` | `Transferring` | `arrow-right-left` | `primary` |
| `adjust` | `Adjust` | `Adjusting` | `sliders` | `secondary` |
| `deposit` | `Deposit` | `Processing` | `arrow-down-left` | `success` |
| `withdrawal` | `Withdrawal` | `Processing` | `arrow-up-right` | `danger` |
| `login` | `Login` | `Verifying` | `arrow-right` | `primary` |
| `logout` | `Logout` | `SigningOut` | `log-out` | `danger` |
| `permissions` | `Permissions` | `Opening` | `shield-check` | `secondary` |
| `demo` | `Demo` | `Loading` | `key-round` | `secondary` |
| `invoice` | `Invoice` | `Opening` | `file-Check-2` | `primary` |
| `challan` | `Challan` | `Opening` | `truck` | `primary` |
| `review` | `Review` | `Opening` | `check-square` | `secondary` |

---

## 3. Reusable Button System (`ERP-design-system.css` & `ERP-core.js`)

### CSS Classes (`src/ERP.Web.Api/wwwroot/css/ERP-design-system.css`)

- **Base & Sizing**:
  - `.erp-btn`: Standard button (`height: 2.25rem` / `36px`, `padding: 0 0.875rem`, `gap: 0.375rem`, `border-radius: 0.75rem`, `font-size: 0.75rem`, `font-weight: 600`).
  - `.erp-btn-sm`: Compact toolbar/panel button (`height: 1.75rem` / `28px`, `padding: 0 0.625rem`, `gap: 0.25rem`, `border-radius: 0.5rem`).
  - `.erp-btn-xs`: Inline table row action button (`height: 1.625rem` / `26px`, `padding: 0 0.625rem`, `gap: 0.25rem`, `border-radius: 0.5rem`, `font-size: 0.6875rem`).
  - `.erp-btn-icon` / `.erp-btn-icon-sm`: Square icon-only controls (`2.25rem` or `1.75rem`) for justified compact toolbar/table/modal controls.
- **Semantic Variants**:
  - `.erp-btn-primary`, `.erp-btn-secondary`, `.erp-btn-success`, `.erp-btn-danger`, `.erp-btn-warning`, `.erp-btn-outline`
  - `.btn-action-edit`, `.btn-action-delete`, `.btn-action-restore`, `.btn-action-audit`, `.btn-action-back`, `.btn-action-cancel`

### JavaScript Helpers (`src/ERP.Web.Api/wwwroot/js/ERP-core.js`)

1. **`renderErpButton(options)`**:
   Generates a standardized `<button>` HTML string from a canonical `action` key:
   ```js
   const html = renderErpButton({
     action: 'add',
     size: 'md',
     id: 'btn-add-record',
     onclick: "openEntryStudio('customer')",
     title: 'Add New Customer Record'
   });
   ```
2. **`setErpButtonLoading(buttonOrId, isLoading, actionKey)`**:
   Toggles the disabled/`aria-busy` state and updates the button's `<span>` text between its canonical single-word label (`Save`) and single-word loading state (`Saving`).
3. **`hydrateErpButtons(root)`**:
   Automatically populates any `<button data-erp-btn="actionKey">` declarative element with its standard icon, one-word label, and accessibility attributes.

---

## 4. Documented & Justified Exceptions

In accordance with Section 3 of the specification, the following controls are explicitly documented exceptions to the visible text word rule:

### A. Icon-Only Controls (Must Have `title` and/or `aria-label`)
1. **Top Header Utility Toggles**:
   - `#sidebar-toggle-btn` (`title="Toggle Sidebar"`, `aria-label="Toggle Sidebar"`)
   - `#search-clear-btn` (`title="Clear search"`, `aria-label="Clear search"`)
   - `#search-toggle-btn` (`title="Search modules & records (Ctrl + K)"`, `aria-label="Search modules and records"`)
   - `#theme-toggle-btn` (`title="Toggle Light / Dark Mode"`, `aria-label="Toggle Light / Dark Mode"`)
   - `#notification-bell-btn` (`title="System Notifications"`, `aria-label="System Notifications"`)
2. **Modal Close (`x`) Controls**:
   - Company Switcher Modal Close (`onclick="closeCompanyModal()"`, `title="Close"`, `aria-label="Close"`)
   - Forensic Audit History Modal Close (`onclick="closeAuditHistoryModal()"`, `title="Close"`, `aria-label="Close"`)
3. **Compact Table Line-Item Row Delete Buttons**:
   - Inline voucher, order, and challan line-item removal controls (`onclick="removeJvLine(this)"`, `onclick="this.closest('tr').remove()"`, and challan minimum-line guard `title="Delete Line"`), which use `<i data-lucide="trash-2"></i>` with `title` and `aria-label` to preserve narrow numeric table column layout.

### B. Special-Purpose Header Context Selectors
1. **`#header-company-switcher-btn`**: Displays the active subsidiary entity badge, full legal name, and dropdown chevron in the global top bar (`aria-label="Switch Company"`).
2. **`#user-profile-btn`**: Displays the authenticated user's avatar initials, full name, role badge, and dropdown chevron in the global top bar (`aria-label="User Profile Menu"`).

---

## 5. Automated Verification Safeguard

`tests/ERP.UnitTests/ButtonStandardizationTests.cs` runs automatically as part of `dotnet test ERP.sln`. It parses every `<button>` across `index.html`, `erp-studios.js`, and `ERP-core.js` and fails the build if:
- Any standard button is missing a Lucide icon (`data-lucide`).
- Any standard button has zero words or more than one visible word in its label.
- Any icon-only button is missing an accessible name (`title` or `aria-label`).
- Any dynamic button text assignment in JS sets a multi-word button label.
