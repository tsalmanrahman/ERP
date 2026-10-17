/**
 * Pyrix Enterprise Core UI Controller
 * Ported for ERP System
 */

document.addEventListener('DOMContentLoaded', () => {
  initTheme();
  initSidebar();
  hydrateErpButtons();
  initLucide();
  initSearch();
  initUserMenu();
  initActiveCompany();
  initGlobalShortcuts();
});

// ==========================================================================
// 1. LUCIDE ICONS ENGINE
// ==========================================================================
function initLucide() {
  if (window.lucide && typeof window.lucide.createIcons === 'function') {
    window.lucide.createIcons();
  }
}

// ==========================================================================
// 2. THEME ENGINE (DARK / LIGHT DUAL-THEME CONTROLLER)
// ==========================================================================
function initTheme() {
  const localTheme = localStorage.getItem('pyrix_theme');
  const cookieMatch = document.cookie.match(/(?:^|;\s*)pyrix_theme=([^;]+)/);
  const cookieTheme = cookieMatch ? decodeURIComponent(cookieMatch[1]) : null;
  const serverTheme = document.documentElement.getAttribute('data-theme');

  const currentTheme = (localTheme === 'dark' || localTheme === 'light') ? localTheme
                     : ((cookieTheme === 'dark' || cookieTheme === 'light') ? cookieTheme
                     : ((serverTheme === 'dark' || serverTheme === 'light') ? serverTheme : 'dark'));

  applyTheme(currentTheme);
}

function toggleTheme() {
  const currentTheme = document.documentElement.getAttribute('data-theme') || 'dark';
  const newTheme = currentTheme === 'light' ? 'dark' : 'light';
  
  applyTheme(newTheme);
  localStorage.setItem('pyrix_theme', newTheme);
  document.cookie = `pyrix_theme=${newTheme};path=/;max-age=31536000;SameSite=Lax`;
  
  showToast(`Switched to ${newTheme === 'dark' ? 'Dark Mode' : 'Light Mode'}`, 'info');
}

function applyTheme(theme) {
  const activeTheme = (theme === 'light') ? 'light' : 'dark';
  const htmlEl = document.documentElement;
  htmlEl.setAttribute('data-theme', activeTheme);
  
  if (activeTheme === 'light') {
    htmlEl.classList.add('light');
    htmlEl.classList.remove('dark');
  } else {
    htmlEl.classList.add('dark');
    htmlEl.classList.remove('light');
  }

  const icon = document.getElementById('theme-toggle-icon');
  if (icon) {
    if (activeTheme === 'dark') {
      icon.setAttribute('data-lucide', 'sun');
      icon.className = 'w-4 h-4 text-amber-400';
    } else {
      icon.setAttribute('data-lucide', 'moon');
      icon.className = 'w-4 h-4 text-indigo-600';
    }
    initLucide();
  }
}

// ==========================================================================
// 3. FLOATING SIDEBAR CONTROLLER (Option D Monaco Architecture)
// ==========================================================================
function initSidebar() {
  const htmlEl = document.documentElement;
  const isSmallScreen = window.innerWidth < 1024;
  const savedCollapsed = localStorage.getItem('pyrix_sidebar_collapsed');
  
  if (!isSmallScreen && savedCollapsed === 'true') {
    htmlEl.classList.add('sidebar-collapsed');
  }

  initSidebarQuickSearch();
}

function toggleSidebar() {
  const htmlEl = document.documentElement;
  const isSmallScreen = window.innerWidth < 1024;

  if (isSmallScreen) {
    // Mobile Drawer Toggle
    htmlEl.classList.toggle('sidebar-open-mobile');
  } else {
    // Desktop Collapse Toggle (Toggles between 268px Expanded & 58px Monaco Slim Rail)
    const isCollapsed = htmlEl.classList.toggle('sidebar-collapsed');
    localStorage.setItem('pyrix_sidebar_collapsed', isCollapsed ? 'true' : 'false');
  }
  initLucide();
}

function closeSidebarMobile() {
  document.documentElement.classList.remove('sidebar-open-mobile');
}

// Generic Module Submenu Tree Accordion Controller for all 11 modules
function toggleModuleAccordion(slug, forceState) {
  if (!slug) return;
  const content = document.getElementById(`accordion-content-${slug}`) || (slug === 'general-ledger' ? document.getElementById('gl-accordion-content') : null);
  const chevron = document.getElementById(`accordion-chevron-${slug}`) || (slug === 'general-ledger' ? document.getElementById('gl-accordion-chevron') : null);
  if (!content) return;

  const isCollapsed = content.classList.contains('collapsed');
  const shouldExpand = (forceState !== undefined) ? forceState : isCollapsed;

  if (shouldExpand) {
    content.classList.remove('collapsed');
    content.classList.add('expanded');
    if (chevron) chevron.style.transform = 'rotate(180deg)';
  } else {
    content.classList.remove('expanded');
    content.classList.add('collapsed');
    if (chevron) chevron.style.transform = 'rotate(0deg)';
  }
}

// Backward-compatible alias for General Ledger
function toggleGlTreeAccordion(forceState) {
  toggleModuleAccordion('general-ledger', forceState);
}

// Instant Quick-Search Filter (Ctrl + K) across all 11 modules & submenus
function filterSidebarItems(query) {
  const q = (query || '').trim().toLowerCase();
  const navItems = document.querySelectorAll('.floating-sidebar .nav-item, .floating-sidebar .tree-sub-item');
  const groups = document.querySelectorAll('.floating-sidebar .sidebar-nav-group');
  const accordionNodes = document.querySelectorAll('.floating-sidebar .sidebar-accordion-node');

  if (!q) {
    navItems.forEach(el => el.style.display = '');
    groups.forEach(g => g.style.display = '');
    accordionNodes.forEach(node => node.style.display = '');
    return;
  }

  const matchedModules = new Set();
  navItems.forEach(el => {
    const text = el.textContent.toLowerCase();
    const matches = text.includes(q);
    el.style.display = matches ? '' : 'none';
    if (matches) {
      const node = el.closest('.sidebar-accordion-node');
      if (node) {
        const slug = node.getAttribute('data-accordion-module');
        if (slug) matchedModules.add(slug);
      }
    }
  });

  // Auto-expand any accordion node that contains matching child items
  matchedModules.forEach(slug => {
    toggleModuleAccordion(slug, true);
  });

  // Keep accordion node visible if either parent or any child matches
  accordionNodes.forEach(node => {
    const visibleItems = node.querySelectorAll('.nav-item:not([style*="display: none"]), .tree-sub-item:not([style*="display: none"])');
    node.style.display = visibleItems.length > 0 ? '' : 'none';
  });

  groups.forEach(g => {
    const visibleChildren = g.querySelectorAll('.nav-item:not([style*="display: none"]), .tree-sub-item:not([style*="display: none"])');
    g.style.display = visibleChildren.length > 0 ? '' : 'none';
  });
}

function initSidebarQuickSearch() {
  const input = document.getElementById('sidebar-quick-search');
  if (input) {
    input.addEventListener('input', (e) => {
      filterSidebarItems(e.target.value);
    });
    input.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') {
        input.value = '';
        filterSidebarItems('');
        input.blur();
      }
    });
  }

  // Global Keyboard Shortcut: Ctrl+K / Cmd+K focuses quick search
  document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
      const login = document.getElementById('erp-login-screen');
      if (login && !login.classList.contains('hidden')) return;

      const input = document.getElementById('sidebar-quick-search');
      if (input && !document.documentElement.classList.contains('sidebar-collapsed')) {
        e.preventDefault();
        input.focus();
        input.select();
      }
    }
  });
}

// ==========================================================================
// 4. SLIDING EXPANDABLE SEARCH COMPONENT
// ==========================================================================
function initSearch() {
  const toggleBtn = document.getElementById('search-toggle-btn');
  const searchBox = document.getElementById('search-slider-box');
  const searchInput = document.getElementById('global-search');
  const clearBtn = document.getElementById('search-clear-btn');

  if (toggleBtn && searchBox && searchInput) {
    toggleBtn.addEventListener('click', (e) => {
      e.stopPropagation();
      toggleHeaderSearch();
    });

    searchInput.addEventListener('input', (e) => {
      const query = e.target.value.trim().toLowerCase();
      if (clearBtn) {
        clearBtn.classList.toggle('hidden', query.length === 0);
      }
      filterModulesCatalog(query);
    });

    searchInput.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') {
        closeHeaderSearch();
      }
    });
  }
}

function toggleHeaderSearch() {
  const searchBox = document.getElementById('search-slider-box');
  const searchInput = document.getElementById('global-search');
  const toggleBtn = document.getElementById('search-toggle-btn');
  if (!searchBox) return;

  const isExpanded = searchBox.classList.contains('expanded');
  if (isExpanded) {
    closeHeaderSearch();
  } else {
    searchBox.classList.add('expanded');
    if (toggleBtn) toggleBtn.classList.add('active');
    setTimeout(() => searchInput && searchInput.focus(), 150);
  }
}

function closeHeaderSearch() {
  const searchBox = document.getElementById('search-slider-box');
  const searchInput = document.getElementById('global-search');
  const toggleBtn = document.getElementById('search-toggle-btn');
  if (searchBox) searchBox.classList.remove('expanded');
  if (toggleBtn) toggleBtn.classList.remove('active');
  if (searchInput) {
    searchInput.value = '';
    const clearBtn = document.getElementById('search-clear-btn');
    if (clearBtn) clearBtn.classList.add('hidden');
    filterModulesCatalog('');
  }
}

function clearHeaderSearch() {
  const searchInput = document.getElementById('global-search');
  const clearBtn = document.getElementById('search-clear-btn');
  if (searchInput) {
    searchInput.value = '';
    searchInput.focus();
  }
  if (clearBtn) clearBtn.classList.add('hidden');
  filterModulesCatalog('');
}

function filterModulesCatalog(query) {
  const cards = document.querySelectorAll('.module-card-item');
  cards.forEach(card => {
    const label = (card.getAttribute('data-label') || '').toLowerCase();
    const domain = (card.getAttribute('data-domain') || '').toLowerCase();
    if (!query || label.includes(query) || domain.includes(query)) {
      card.style.display = 'flex';
    } else {
      card.style.display = 'none';
    }
  });
}

// ==========================================================================
// 5. USER PROFILE DROPDOWN MENU
// ==========================================================================
function initUserMenu() {
  const btn = document.getElementById('user-profile-btn');
  const dropdown = document.getElementById('user-profile-dropdown');
  const container = document.getElementById('user-profile-menu-container');

  if (btn && dropdown) {
    btn.addEventListener('click', (e) => {
      e.preventDefault();
      e.stopPropagation();
      toggleUserMenu();
    });

    dropdown.addEventListener('click', (e) => e.stopPropagation());
  }

  document.addEventListener('click', (e) => {
    if (container && dropdown && !container.contains(e.target)) {
      closeUserMenu();
    }
  });
}

function toggleUserMenu() {
  const dropdown = document.getElementById('user-profile-dropdown');
  const chevron = document.getElementById('user-profile-chevron');
  if (!dropdown) return;

  const isHidden = dropdown.classList.contains('hidden');
  if (isHidden) {
    dropdown.classList.remove('hidden');
    if (chevron) chevron.classList.add('open');
  } else {
    dropdown.classList.add('hidden');
    if (chevron) chevron.classList.remove('open');
  }
  initLucide();
}

function closeUserMenu() {
  const dropdown = document.getElementById('user-profile-dropdown');
  const chevron = document.getElementById('user-profile-chevron');
  if (dropdown) dropdown.classList.add('hidden');
  if (chevron) chevron.classList.remove('open');
}

// ==========================================================================
// 6. GLOBAL SUBSIDIARY / COMPANY SWITCHER MODAL
// ==========================================================================
function openCompanyModal() {
  if (ERP_STATE.isCompanyLocked) {
    showToast(ERP_STATE.companyLockReason || 'Company switching is disabled while viewing or editing a record. Return to list or dashboard to switch companies.', 'warning');
    return;
  }
  const modal = document.getElementById('company-modal');
  if (modal) {
    modal.classList.add('active');
    initLucide();
  }
}

function closeCompanyModal() {
  const modal = document.getElementById('company-modal');
  if (modal) {
    modal.classList.remove('active');
  }
}

function lockCompanySwitcher(isLocked, reason = null) {
  ERP_STATE.isCompanyLocked = !!isLocked;
  ERP_STATE.companyLockReason = reason;

  const btn = document.getElementById('header-company-switcher-btn');
  const lockIcon = document.getElementById('header-company-lock-icon');

  if (btn) {
    if (isLocked) {
      btn.classList.add('company-switcher-locked');
      btn.setAttribute('title', reason || 'Company switching locked while viewing or editing record');
      btn.setAttribute('aria-disabled', 'true');
    } else {
      btn.classList.remove('company-switcher-locked');
      btn.setAttribute('title', 'Switch Active Business Entity (Ctrl + Shift + C)');
      btn.removeAttribute('aria-disabled');
    }
  }

  if (lockIcon) {
    if (isLocked) {
      lockIcon.classList.remove('hidden');
    } else {
      lockIcon.classList.add('hidden');
    }
    initLucide();
  }
}

function triggerFieldShake(element) {
  if (!element) return;
  element.classList.remove('erp-shake');
  void element.offsetWidth; // Force reflow
  element.classList.add('erp-shake');
  setTimeout(() => {
    element.classList.remove('erp-shake');
  }, 450);
}

function focusFirstInvalidField(container = document) {
  const firstError = container.querySelector('.input-error');
  if (firstError) {
    firstError.focus();
    if (typeof firstError.scrollIntoView === 'function') {
      firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
  }
}

function updateEntryCompanyDisplays(companyName) {
  const ids = [
    'cust-company-display',
    'vend-company-display',
    'prod-company-display',
    'acc-company-display',
    'wh-company-display',
    'emp-company-display',
    'contra-company-display',
    'challan-company-display',
    'inv-company-display',
    'pr-company-display',
    'st-company-display',
    'bank-company-display',
    'asset-company-display',
    'bom-company-display',
    'pay-company-display'
  ];
  ids.forEach(id => {
    const el = document.getElementById(id);
    if (el) el.value = companyName;
  });
}

function selectCompany(companyId, companyName, companyCode) {
  const nameEl = document.getElementById('header-company-name');
  const codeEl = document.getElementById('header-company-code');
  const heroNameEl = document.getElementById('hero-company-name');
  const heroCodeEl = document.getElementById('hero-company-code');

  if (nameEl) nameEl.textContent = companyName;
  if (codeEl) codeEl.textContent = companyCode;
  if (heroNameEl) heroNameEl.textContent = companyName;
  if (heroCodeEl) heroCodeEl.textContent = companyCode;
  
  localStorage.setItem('erp_active_company_id', companyId);
  localStorage.setItem('erp_active_company_name', companyName);
  localStorage.setItem('erp_active_company_code', companyCode);
  
  updateEntryCompanyDisplays(companyName);
  closeCompanyModal();
  showToast(`Switched active entity to ${companyName} (${companyCode})`, 'success');
}

function initActiveCompany() {
  const savedName = localStorage.getItem('erp_active_company_name') || 'HATIM STEEL STRUCTURE LTD (HSF)';
  const savedCode = localStorage.getItem('erp_active_company_code') || 'HSF';
  
  const nameEl = document.getElementById('header-company-name');
  const codeEl = document.getElementById('header-company-code');
  const heroNameEl = document.getElementById('hero-company-name');
  const heroCodeEl = document.getElementById('hero-company-code');

  if (nameEl) nameEl.textContent = savedName;
  if (codeEl) codeEl.textContent = savedCode;
  if (heroNameEl) heroNameEl.textContent = savedName;
  if (heroCodeEl) heroCodeEl.textContent = savedCode;

  updateEntryCompanyDisplays(savedName);
}

// ==========================================================================
// 7. TOAST NOTIFICATIONS ENGINE
// ==========================================================================
function showToast(message, type = 'info') {
  let existing = document.querySelector('.pyrix-toast');
  if (existing) existing.remove();

  const toast = document.createElement('div');
  toast.className = 'pyrix-toast';
  
  let iconName = 'info';
  let iconColor = 'text-blue-400';
  if (type === 'success') {
    iconName = 'check-circle-2';
    iconColor = 'text-emerald-400';
  } else if (type === 'error') {
    iconName = 'alert-triangle';
    iconColor = 'text-rose-400';
  } else if (type === 'warning') {
    iconName = 'alert-circle';
    iconColor = 'text-amber-400';
  }

  toast.innerHTML = `
    <i data-lucide="${iconName}" class="w-4 h-4 ${iconColor} shrink-0"></i>
    <span class="text-xs font-semibold">${message}</span>
  `;

  document.body.appendChild(toast);
  initLucide();

  if (type === 'warning' || type === 'error') {
    if (typeof focusFirstInvalidField === 'function') {
      focusFirstInvalidField();
    }
  }

  setTimeout(() => {
    toast.style.transition = 'opacity 0.25s ease, transform 0.25s ease';
    toast.style.opacity = '0';
    toast.style.transform = 'translateY(10px) scale(0.96)';
    setTimeout(() => toast.remove(), 250);
  }, 3200);
}

// ==========================================================================
// 8. GLOBAL KEYBOARD SHORTCUTS
// ==========================================================================
function initGlobalShortcuts() {
  document.addEventListener('keydown', (e) => {
    // Ctrl + B: Toggle Sidebar
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'b') {
      e.preventDefault();
      toggleSidebar();
    }
    // Ctrl + K: Toggle Search
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault();
      toggleHeaderSearch();
    }
    // Ctrl + Shift + C: Toggle Company Switcher
    if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key.toLowerCase() === 'c') {
      e.preventDefault();
      openCompanyModal();
    }
    // Ctrl + Alt + N: Universal Master Record Create Modal
    if ((e.ctrlKey || e.metaKey) && e.altKey && e.key.toLowerCase() === 'n') {
      e.preventDefault();
      if (typeof openRecordCreateModal === 'function') openRecordCreateModal('customer');
    }
    // Escape: Close all dropdowns and modals
    if (e.key === 'Escape') {
      closeUserMenu();
      closeHeaderSearch();
      closeCompanyModal();
      if (typeof closeRecordCreateModal === 'function') closeRecordCreateModal();
      closeSidebarMobile();
      closeAuditModal();
      closeSoftDeleteModal();
    }
  });
}

// ==========================================================================
// 9. ENTERPRISE SOFT-DELETE & AUDIT TRAIL MODAL CONTROLLER
// ==========================================================================
function confirmSoftDelete(entityType, id, displayCode, onConfirm) {
  let modal = document.getElementById('pyrix-soft-delete-modal');
  if (!modal) {
    modal = document.createElement('div');
    modal.id = 'pyrix-soft-delete-modal';
    modal.className = 'fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm hidden';
    modal.innerHTML = `
      <div class="bg-card border border-border rounded-xl shadow-2xl max-w-md w-full p-6 space-y-4 animate-in fade-in zoom-in-95">
        <div class="flex items-center gap-3 text-rose-500">
          <div class="p-2 rounded-lg bg-rose-500/10"><i data-lucide="alert-triangle" class="w-6 h-6"></i></div>
          <h3 class="text-base font-bold text-foreground">Confirm Record Soft Deletion</h3>
        </div>
        <p class="text-xs text-muted-foreground leading-relaxed" id="soft-delete-message">
          Are you sure you want to soft-delete this record? It will be safely archived and excluded from active views.
        </p>
        <div class="p-3 bg-secondary/50 rounded-lg text-xs font-mono font-semibold text-foreground/80 flex items-center justify-between">
          <span class="text-muted-foreground">Entity:</span>
          <span id="soft-delete-code" class="text-rose-400"></span>
        </div>
        <div class="flex items-center justify-end gap-2 pt-2 border-t border-border/50">
          <button type="button" onclick="closeSoftDeleteModal()" class="erp-btn btn-action-cancel" title="Cancel Deletion" aria-label="Cancel">
            <i data-lucide="x" class="w-3.5 h-3.5"></i>
            <span>Cancel</span>
          </button>
          <button type="button" id="soft-delete-confirm-btn" class="erp-btn erp-btn-destructive-solid" title="Confirm Record Soft Deletion" aria-label="Delete">
            <i data-lucide="trash-2" class="w-3.5 h-3.5"></i>
            <span>Delete</span>
          </button>
        </div>
      </div>
    `;
    document.body.appendChild(modal);
  }

  document.getElementById('soft-delete-code').textContent = `${entityType}: ${displayCode}`;
  document.getElementById('soft-delete-message').textContent = 
    `Are you sure you want to soft-delete record '${displayCode}'? It will preserve double-entry audit history and can be restored by an authorized administrator.`;
  
  const confirmBtn = document.getElementById('soft-delete-confirm-btn');
  confirmBtn.onclick = async () => {
    closeSoftDeleteModal();
    if (typeof onConfirm === 'function') await onConfirm();
  };

  modal.classList.remove('hidden');
  initLucide();
}

function closeSoftDeleteModal() {
  const modal = document.getElementById('pyrix-soft-delete-modal');
  if (modal) modal.classList.add('hidden');
}

async function viewAuditHistoryModal(entityName, sysGuid, displayCode) {
  let modal = document.getElementById('pyrix-audit-history-modal');
  if (!modal) {
    modal = document.createElement('div');
    modal.id = 'pyrix-audit-history-modal';
    modal.className = 'fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm hidden';
    modal.innerHTML = `
      <div class="bg-card border border-border rounded-xl shadow-2xl max-w-2xl w-full max-h-[85vh] flex flex-col p-6 space-y-4 animate-in fade-in zoom-in-95">
        <div class="flex items-center justify-between border-b border-border/50 pb-3">
          <div class="flex items-center gap-2.5">
            <div class="p-2 rounded-lg bg-indigo-500/10 text-indigo-400"><i data-lucide="history" class="w-5 h-5"></i></div>
            <div>
              <h3 class="text-sm font-bold text-foreground">Immutable Audit Trail</h3>
              <p class="text-[11px] text-muted-foreground font-mono" id="audit-modal-subtitle"></p>
            </div>
          </div>
          <button type="button" onclick="closeAuditModal()" class="erp-btn-icon-sm hover:bg-secondary text-muted-foreground hover:text-foreground transition" title="Close Audit Trail Modal" aria-label="Close">
            <i data-lucide="x" class="w-4 h-4"></i>
          </button>
        </div>
        <div class="overflow-y-auto flex-1 pr-1 space-y-3" id="audit-timeline-container">
          <div class="text-center py-8 text-xs text-muted-foreground">Loading audit history...</div>
        </div>
        <div class="flex items-center justify-between pt-3 border-t border-border/50 text-xs text-muted-foreground">
          <span>Source: <strong class="text-foreground">sa.AuditLogs</strong></span>
          <div class="flex items-center gap-2">
            <button type="button" onclick="closeAuditModal(); if (typeof navigateToStudio === 'function') navigateToStudio('audit-trail');" class="erp-btn erp-btn-primary bg-indigo-600 hover:bg-indigo-500 text-white" title="Open Dedicated Audit Studio" aria-label="Open Dedicated Audit Studio">
              <i data-lucide="maximize-2" class="w-3.5 h-3.5"></i>
              <span>Audit</span>
            </button>
            <button type="button" onclick="closeAuditModal()" class="erp-btn btn-action-cancel" title="Close Audit Trail Modal" aria-label="Close">
              <i data-lucide="x" class="w-3.5 h-3.5"></i>
              <span>Close</span>
            </button>
          </div>
        </div>
      </div>
    `;
    document.body.appendChild(modal);
  }

  document.getElementById('audit-modal-subtitle').textContent = `${entityName} • ${displayCode || sysGuid}`;
  modal.classList.remove('hidden');
  initLucide();

  const container = document.getElementById('audit-timeline-container');
  container.innerHTML = '<div class="text-center py-8 text-xs text-muted-foreground">Loading audit trail...</div>';

  try {
    const res = await apiFetch(`/api/sa/audit/entity/${entityName}/${sysGuid}`);
    const json = await res.json();
    if (!json.success || !json.data || json.data.length === 0) {
      container.innerHTML = '<div class="text-center py-8 text-xs text-muted-foreground">No audit entries recorded for this record yet.</div>';
      return;
    }

    container.innerHTML = json.data.map(log => {
      const localTime = new Date(log.timestampUtc).toLocaleString();
      let badgeClass = 'bg-blue-500/10 text-blue-400 border-blue-500/20';
      if (log.action === 'Create') badgeClass = 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      else if (log.action === 'Delete') badgeClass = 'bg-rose-500/10 text-rose-400 border-rose-500/20';
      else if (log.action === 'Restore') badgeClass = 'bg-amber-500/10 text-amber-400 border-amber-500/20';

      return `
        <div class="p-3.5 rounded-lg bg-secondary/30 border border-border/50 text-xs space-y-2">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border ${badgeClass}">${log.action}</span>
              <span class="font-semibold text-foreground">${log.userName}</span>
            </div>
            <span class="text-[11px] text-muted-foreground font-mono">${localTime}</span>
          </div>
          ${log.affectedColumns ? `<div class="text-[11px] text-muted-foreground"><strong class="text-foreground/80">Modified Columns:</strong> ${log.affectedColumns}</div>` : ''}
          ${log.newValues ? `
            <details class="text-[11px]">
              <summary class="cursor-pointer text-indigo-400 hover:underline">View Snapshot Payload</summary>
              <pre class="mt-1 p-2 rounded bg-black/40 font-mono text-[10px] overflow-x-auto text-emerald-300 max-h-32">${JSON.stringify(JSON.parse(log.newValues), null, 2)}</pre>
            </details>
          ` : ''}
        </div>
      `;
    }).join('');
  } catch (err) {
    container.innerHTML = `<div class="text-center py-8 text-xs text-rose-400">Failed to load audit history: ${err.message}</div>`;
  }
}

function closeAuditModal() {
  const modal = document.getElementById('pyrix-audit-history-modal');
  if (modal) modal.classList.add('hidden');
}

// ==========================================================================
// 10. CENTRALIZED REUSABLE BUTTON SYSTEM (ICON + ONE-WORD LABEL STANDARD)
// ==========================================================================
const ERP_BUTTON_STANDARDS = Object.freeze({
  add:         { label: 'Add',         icon: 'plus',             variant: 'btn-action-add',          loadingLabel: 'Adding' },
  edit:        { label: 'Edit',        icon: 'edit-2',           variant: 'btn-action-edit',         loadingLabel: 'Updating' },
  save:        { label: 'Save',        icon: 'check',            variant: 'btn-action-save',         loadingLabel: 'Saving' },
  cancel:      { label: 'Cancel',      icon: 'x',                variant: 'btn-action-cancel',       loadingLabel: 'Canceling' },
  back:        { label: 'Back',        icon: 'arrow-left',       variant: 'btn-action-back',         loadingLabel: 'Back' },
  delete:      { label: 'Delete',      icon: 'trash-2',          variant: 'btn-action-delete',       loadingLabel: 'Deleting' },
  discard:     { label: 'Discard',     icon: 'trash-2',          variant: 'erp-btn-destructive-solid', loadingLabel: 'Discarding' },
  search:      { label: 'Search',      icon: 'search',           variant: 'btn-action-secondary',    loadingLabel: 'Searching' },
  filter:      { label: 'Filter',      icon: 'filter',           variant: 'btn-action-secondary',    loadingLabel: 'Filtering' },
  reset:       { label: 'Reset',       icon: 'rotate-ccw',       variant: 'btn-action-reset',        loadingLabel: 'Resetting' },
  refresh:     { label: 'Refresh',     icon: 'refresh-cw',       variant: 'btn-action-refresh',      loadingLabel: 'Refreshing' },
  view:        { label: 'View',        icon: 'eye',              variant: 'btn-action-secondary',    loadingLabel: 'Loading' },
  review:      { label: 'Review',      icon: 'eye',              variant: 'btn-action-secondary',    loadingLabel: 'Reviewing' },
  details:     { label: 'Details',     icon: 'file-diff',        variant: 'btn-action-secondary',    loadingLabel: 'Loading' },
  approve:     { label: 'Approve',     icon: 'check',            variant: 'erp-btn-primary',         loadingLabel: 'Approving' },
  reject:      { label: 'Reject',      icon: 'x',                variant: 'btn-action-delete',       loadingLabel: 'Rejecting' },
  submit:      { label: 'Submit',      icon: 'send',             variant: 'erp-btn-primary',         loadingLabel: 'Submitting' },
  confirm:     { label: 'Confirm',     icon: 'check-circle-2',   variant: 'erp-btn-primary',         loadingLabel: 'Confirming' },
  print:       { label: 'Print',       icon: 'printer',          variant: 'btn-action-print',        loadingLabel: 'Printing' },
  export:      { label: 'Export',      icon: 'download',         variant: 'btn-action-export',       loadingLabel: 'Exporting' },
  import:      { label: 'Import',      icon: 'upload',           variant: 'btn-action-secondary',    loadingLabel: 'Importing' },
  download:    { label: 'Download',    icon: 'download',         variant: 'erp-btn-primary',         loadingLabel: 'Downloading' },
  upload:      { label: 'Upload',      icon: 'upload',           variant: 'btn-action-secondary',    loadingLabel: 'Uploading' },
  post:        { label: 'Post',        icon: 'check',            variant: 'btn-action-post',         loadingLabel: 'Posting' },
  reverse:     { label: 'Reverse',     icon: 'rotate-ccw',       variant: 'btn-action-delete',       loadingLabel: 'Reversing' },
  restore:     { label: 'Restore',     icon: 'rotate-ccw',       variant: 'erp-btn-primary',         loadingLabel: 'Restoring' },
  generate:    { label: 'Generate',    icon: 'sparkles',         variant: 'erp-btn-primary',         loadingLabel: 'Generating' },
  close:       { label: 'Close',       icon: 'x',                variant: 'btn-action-cancel',       loadingLabel: 'Closing' },
  settings:    { label: 'Settings',    icon: 'settings',         variant: 'btn-action-secondary',    loadingLabel: 'Loading' },
  balance:     { label: 'Balance',     icon: 'scale',            variant: 'btn-action-secondary',    loadingLabel: 'Balancing' },
  verify:      { label: 'Verify',      icon: 'scale',            variant: 'btn-action-secondary',    loadingLabel: 'Verifying' },
  allocate:    { label: 'Allocate',    icon: 'check-circle-2',   variant: 'btn-action-secondary',    loadingLabel: 'Allocating' },
  notify:      { label: 'Notify',      icon: 'mail-warning',     variant: 'btn-action-warning',      loadingLabel: 'Notifying' },
  pay:         { label: 'Pay',         icon: 'wallet',           variant: 'btn-action-delete',       loadingLabel: 'Paying' },
  transfer:    { label: 'Transfer',    icon: 'arrow-left-right', variant: 'erp-btn-primary',         loadingLabel: 'Transferring' },
  adjust:      { label: 'Adjust',      icon: 'sliders',          variant: 'btn-action-secondary',    loadingLabel: 'Adjusting' },
  deposit:     { label: 'Deposit',     icon: 'arrow-up-right',   variant: 'btn-action-secondary',    loadingLabel: 'Depositing' },
  withdrawal:  { label: 'Withdrawal',  icon: 'arrow-down-left',  variant: 'btn-action-secondary',    loadingLabel: 'Withdrawing' },
  login:       { label: 'Login',       icon: 'log-in',           variant: 'erp-btn-primary',         loadingLabel: 'Verifying' },
  logout:      { label: 'Logout',      icon: 'log-out',          variant: 'btn-action-delete',       loadingLabel: 'Signing' },
  permissions: { label: 'Permissions', icon: 'shield-check',     variant: 'btn-action-secondary',    loadingLabel: 'Loading' },
  demo:        { label: 'Demo',        icon: 'key-round',        variant: 'erp-btn-ghost',           loadingLabel: 'Filling' },
  invoice:     { label: 'Invoice',     icon: 'printer',          variant: 'btn-action-print',        loadingLabel: 'Printing' },
  challan:     { label: 'Challan',     icon: 'truck',            variant: 'btn-action-post',         loadingLabel: 'Printing' },
  audit:       { label: 'Audit',       icon: 'maximize-2',       variant: 'erp-btn-primary',         loadingLabel: 'Opening' }
});

/**
 * Generates standardized ERP button markup enforcing the Icon + One-Word Label convention.
 * @param {Object} options - Button configuration
 * @param {string} [options.action] - Canonical key in ERP_BUTTON_STANDARDS
 * @param {string} [options.label] - One-word label override (validated as single word)
 * @param {string} [options.icon] - Lucide icon name override
 * @param {string} [options.variant] - CSS variant class override
 * @param {string} [options.size] - 'sm', 'xs', or '' (default 36px)
 * @param {string} [options.onClick] - Inline onclick handler string
 * @param {string} [options.title] - Accessible tooltip & aria-label description
 * @param {string} [options.id] - DOM element ID
 * @param {string} [options.textId] - DOM ID for the inner <span> label
 * @param {string} [options.type] - 'button' (default) or 'submit'
 * @param {boolean} [options.iconOnly] - True only for justified compact/toolbar icon-only controls
 * @param {string} [options.className] - Additional CSS utility classes
 */
function renderErpButton(options = {}) {
  const actionKey = (options.action || 'save').toLowerCase();
  const std = ERP_BUTTON_STANDARDS[actionKey] || ERP_BUTTON_STANDARDS.save;
  const rawLabel = (options.label !== undefined ? String(options.label) : std.label).trim();
  const iconName = options.icon || std.icon;
  const variantClass = options.variant || std.variant;
  const typeAttr = options.type || 'button';
  const sizeClass = options.size === 'sm' ? 'erp-btn-sm' : (options.size === 'xs' ? 'erp-btn-xs' : '');
  const iconSizeClass = (options.size === 'sm' || options.size === 'xs') ? 'w-3 h-3' : 'w-3.5 h-3.5';
  const extraClass = options.className || '';
  const idAttr = options.id ? ` id="${options.id}"` : '';
  const onClickAttr = options.onClick ? ` onclick="${options.onClick}"` : '';
  const accessibleName = options.title || rawLabel;
  const titleAttr = accessibleName ? ` title="${accessibleName}" aria-label="${accessibleName}"` : '';

  if (options.iconOnly) {
    const iconBtnClass = options.size === 'sm' ? 'erp-btn-icon-sm' : 'erp-btn-icon';
    const fullIconClass = `${iconBtnClass} ${extraClass}`.trim();
    return `<button type="${typeAttr}"${idAttr}${onClickAttr} class="${fullIconClass}"${titleAttr}><i data-lucide="${iconName}" class="${iconSizeClass}"></i></button>`;
  }

  const singleWordLabel = rawLabel.split(/\s+/)[0] || std.label;
  const spanIdAttr = options.textId ? ` id="${options.textId}"` : '';
  const fullBtnClass = `erp-btn ${variantClass} ${sizeClass} ${extraClass}`.replace(/\s+/g, ' ').trim();

  return `<button type="${typeAttr}"${idAttr}${onClickAttr} class="${fullBtnClass}"${titleAttr}><i data-lucide="${iconName}" class="${iconSizeClass}"></i><span${spanIdAttr}>${singleWordLabel}</span></button>`;
}

/**
 * Toggles button loading/disabled state while preserving the Icon + One-Word Label convention.
 */
function setErpButtonLoading(buttonOrId, isLoading, actionKey = 'save') {
  const btn = typeof buttonOrId === 'string' ? document.getElementById(buttonOrId) : buttonOrId;
  if (!btn) return;
  const std = ERP_BUTTON_STANDARDS[(actionKey || 'save').toLowerCase()] || ERP_BUTTON_STANDARDS.save;
  const labelSpan = btn.querySelector('span');

  btn.disabled = Boolean(isLoading);
  if (isLoading) {
    btn.setAttribute('aria-busy', 'true');
    if (labelSpan) labelSpan.textContent = std.loadingLabel;
  } else {
    btn.removeAttribute('aria-busy');
    if (labelSpan) labelSpan.textContent = std.label;
  }
}

/**
 * Hydrates declarative button[data-erp-action="..."] elements into standardized Icon + One-Word Label buttons.
 */
function hydrateErpButtons(root = document) {
  if (!root || typeof root.querySelectorAll !== 'function') return;
  root.querySelectorAll('button[data-erp-action]').forEach(btn => {
    const actionKey = (btn.getAttribute('data-erp-action') || '').toLowerCase();
    const std = ERP_BUTTON_STANDARDS[actionKey];
    if (!std) return;
    btn.classList.add('erp-btn', std.variant);
    if (!btn.getAttribute('title')) btn.setAttribute('title', std.label);
    if (!btn.getAttribute('aria-label')) btn.setAttribute('aria-label', btn.getAttribute('title') || std.label);
    if (!btn.querySelector('[data-lucide]') && !btn.querySelector('svg')) {
      btn.innerHTML = `<i data-lucide="${std.icon}" class="w-3.5 h-3.5"></i><span>${std.label}</span>`;
    }
  });
}

window.ERP_BUTTON_STANDARDS = ERP_BUTTON_STANDARDS;
window.renderErpButton = renderErpButton;
window.setErpButtonLoading = setErpButtonLoading;
window.hydrateErpButtons = hydrateErpButtons;

// ============================================================================
// 11. ADAPTIVE LIST PAGINATION & COMPACT TABLE ENGINE
// ============================================================================
window.ERP_PAGINATION = window.ERP_PAGINATION || {};

class ErpAdaptiveTablePagination {
  constructor(options) {
    this.tableId = options.tableId;
    this.tbodyId = options.tbodyId;
    this.paginationId = options.paginationId;
    this.renderRow = options.renderRow;
    this.data = options.data || [];
    this.currentPage = 1;
    this.pageSizeSetting = 'auto';
    this.minPageSize = options.minPageSize || 5;
    this.maxPageSize = options.maxPageSize || 50;
    this.estimatedRowHeight = options.rowHeight || 38;
    this.onPageChange = options.onPageChange || null;

    this.onResize = this.debounce(() => {
      if (this.pageSizeSetting === 'auto') {
        this.render();
      }
    }, 120);

    window.addEventListener('resize', this.onResize);
    window.ERP_PAGINATION[this.tbodyId] = this;
  }

  setData(data) {
    this.data = data || [];
    const totalPages = this.getTotalPages();
    if (this.currentPage > totalPages) {
      this.currentPage = Math.max(1, totalPages);
    }
    this.render();
  }

  calculateAdaptivePageSize() {
    const tableEl = document.getElementById(this.tableId) || document.getElementById(this.tbodyId);
    const viewportHeight = window.innerHeight || 800;
    let topOffset = 260;
    if (tableEl) {
      const rect = tableEl.getBoundingClientRect();
      if (rect.top > 0) {
        topOffset = rect.top;
      }
    }
    const reservedHeight = 130;
    const availableHeight = viewportHeight - topOffset - reservedHeight;
    const calculated = Math.floor(availableHeight / this.estimatedRowHeight);
    return Math.max(this.minPageSize, Math.min(this.maxPageSize, calculated));
  }

  getPageSize() {
    if (this.pageSizeSetting === 'auto') {
      return this.calculateAdaptivePageSize();
    }
    return parseInt(this.pageSizeSetting, 10) || 15;
  }

  getTotalPages() {
    const size = this.getPageSize();
    return Math.max(1, Math.ceil(this.data.length / size));
  }

  setPage(page) {
    const total = this.getTotalPages();
    this.currentPage = Math.max(1, Math.min(total, page));
    this.render();
    if (typeof this.onPageChange === 'function') {
      this.onPageChange(this.currentPage, this.getPageSize());
    }
  }

  setPageSize(size) {
    this.pageSizeSetting = size;
    const total = this.getTotalPages();
    if (this.currentPage > total) {
      this.currentPage = total;
    }
    this.render();
  }

  render() {
    const tbody = document.getElementById(this.tbodyId);
    const paginationContainer = document.getElementById(this.paginationId);
    if (!tbody) return;

    const pageSize = this.getPageSize();
    const totalRecords = this.data.length;
    const totalPages = Math.max(1, Math.ceil(totalRecords / pageSize));
    if (this.currentPage > totalPages) this.currentPage = totalPages;

    const startIndex = (this.currentPage - 1) * pageSize;
    const endIndex = Math.min(totalRecords, startIndex + pageSize);
    const pageItems = this.data.slice(startIndex, endIndex);

    if (totalRecords === 0) {
      tbody.innerHTML = '';
      if (paginationContainer) paginationContainer.innerHTML = '';
      return;
    }

    tbody.innerHTML = pageItems.map((item, idx) => this.renderRow(item, startIndex + idx)).join('');
    if (typeof window.initLucide === 'function') window.initLucide();

    if (!paginationContainer) return;

    const adaptiveSize = this.calculateAdaptivePageSize();
    const prevDisabled = this.currentPage <= 1 ? 'disabled' : '';
    const nextDisabled = this.currentPage >= totalPages ? 'disabled' : '';

    paginationContainer.innerHTML = `
      <div class="flex items-center gap-3">
        <span>Showing <strong class="text-white">${startIndex + 1}–${endIndex}</strong> of <strong class="text-white">${totalRecords}</strong> records</span>
        <div class="h-4 w-px bg-white/10 hidden sm:block"></div>
        <div class="flex items-center gap-1.5">
          <label class="text-[11px] text-slate-400">Rows per page:</label>
          <select class="erp-page-size-select" onchange="window.ERP_PAGINATION['${this.tbodyId}']?.setPageSize(this.value)">
            <option value="auto"${this.pageSizeSetting === 'auto' ? ' selected' : ''}>Auto (${adaptiveSize})</option>
            <option value="10"${this.pageSizeSetting === '10' ? ' selected' : ''}>10</option>
            <option value="15"${this.pageSizeSetting === '15' ? ' selected' : ''}>15</option>
            <option value="25"${this.pageSizeSetting === '25' ? ' selected' : ''}>25</option>
            <option value="50"${this.pageSizeSetting === '50' ? ' selected' : ''}>50</option>
            <option value="100"${this.pageSizeSetting === '100' ? ' selected' : ''}>100</option>
          </select>
        </div>
      </div>
      <div class="flex items-center gap-1">
        <button type="button" class="erp-btn-xs erp-btn-secondary" onclick="window.ERP_PAGINATION['${this.tbodyId}']?.setPage(${this.currentPage - 1})" ${prevDisabled} title="Previous Page" aria-label="Previous Page">
          <i data-lucide="chevron-left" class="w-3.5 h-3.5"></i>
        </button>
        <span class="px-2 py-0.5 rounded text-xs text-slate-300 font-mono">Page <strong class="text-white">${this.currentPage}</strong> of <strong class="text-white">${totalPages}</strong></span>
        <button type="button" class="erp-btn-xs erp-btn-secondary" onclick="window.ERP_PAGINATION['${this.tbodyId}']?.setPage(${this.currentPage + 1})" ${nextDisabled} title="Next Page" aria-label="Next Page">
          <i data-lucide="chevron-right" class="w-3.5 h-3.5"></i>
        </button>
      </div>
    `;
    if (typeof window.initLucide === 'function') window.initLucide();
  }

  destroy() {
    window.removeEventListener('resize', this.onResize);
    delete window.ERP_PAGINATION[this.tbodyId];
  }

  debounce(fn, wait) {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn.apply(this, args), wait);
    };
  }
}

window.ErpAdaptiveTablePagination = ErpAdaptiveTablePagination;


