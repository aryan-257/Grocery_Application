import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface UserAdmin { id:string; email:string; firstName:string; lastName:string; role:string; phoneNumber:string|null; isActive:boolean; createdAt:string; }
interface UserStats { total:number; active:number; inactive:number; byRole:{role:string;count:number}[]; }

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [FormsModule, CommonModule, DatePipe],
  template: `
<div class="page">
  <div class="page-header">
    <div>
      <h1 class="page-title">User Management</h1>
      <p class="page-sub">Manage all registered users</p>
    </div>
    <span class="result-count">{{ filtered().length }} users</span>
  </div>

  <!-- Stats -->
  @if (stats()) {
    <div class="stats">
      <div class="stat-card c1"><div class="stat-icon">👥</div><div><div class="stat-val">{{ stats()!.total }}</div><div class="stat-lbl">Total Users</div></div></div>
      <div class="stat-card c2"><div class="stat-icon">✅</div><div><div class="stat-val">{{ stats()!.active }}</div><div class="stat-lbl">Active</div></div></div>
      <div class="stat-card c3"><div class="stat-icon">🚫</div><div><div class="stat-val">{{ stats()!.inactive }}</div><div class="stat-lbl">Inactive</div></div></div>
      <div class="stat-card c4"><div class="stat-icon">🛒</div><div><div class="stat-val">{{ roleCount('Customer') }}</div><div class="stat-lbl">Customers</div></div></div>
    </div>
  }

  <!-- Filters -->
  <div class="filters">
    <div class="search-wrap">
      <span class="search-icon">🔍</span>
      <input class="search" [(ngModel)]="search" (ngModelChange)="applyFilters()" placeholder="Search name or email..." />
    </div>
    <select class="fsel" [(ngModel)]="filterRole" (ngModelChange)="applyFilters()">
      <option value="">All Roles</option>
      <option value="Admin">Admin</option>
      <option value="StoreManager">Store Manager</option>
      <option value="DeliveryDriver">Delivery Driver</option>
      <option value="Customer">Customer</option>
    </select>
    <select class="fsel" [(ngModel)]="filterActive" (ngModelChange)="applyFilters()">
      <option value="">All Status</option>
      <option value="true">Active</option>
      <option value="false">Inactive</option>
    </select>
  </div>

  <!-- Table -->
  <div class="table-card">
    @if (loading()) {
      <div class="loading-rows">@for (i of [1,2,3,4]; track i) { <div class="skeleton"></div> }</div>
    } @else if (error()) {
      <div class="empty"><span>⚠️</span><p>{{ error() }}</p><button class="retry-btn" (click)="load()">Retry</button></div>
    } @else if (filtered().length === 0) {
      <div class="empty"><span>📭</span><p>No users found</p></div>
    } @else {
      <table>
        <thead><tr><th>USER</th><th>ROLE</th><th>STATUS</th><th>JOINED</th><th>ACTIONS</th></tr></thead>
        <tbody>
          @for (u of filtered(); track u.id; let i = $index) {
            <tr [class.row-alt]="i % 2 === 1">
              <td>
                <div class="user-cell">
                  <div class="avatar" [class]="'av-' + u.role.toLowerCase()">{{ initials(u) }}</div>
                  <div><p class="uname">{{ u.firstName }} {{ u.lastName }}</p><p class="uemail">{{ u.email }}</p></div>
                </div>
              </td>
              <td>
                @if (editingRole === u.id) {
                  <select [(ngModel)]="newRole" (change)="saveRole(u)" class="role-select">
                    <option value="Admin">Admin</option>
                    <option value="StoreManager">StoreManager</option>
                    <option value="DeliveryDriver">DeliveryDriver</option>
                    <option value="Customer">Customer</option>
                  </select>
                } @else {
                  <span class="role-chip" [class]="'r-' + u.role.toLowerCase()" (click)="startEditRole(u)">{{ u.role }}</span>
                }
              </td>
              <td><span class="status-chip" [class]="u.isActive ? 's-active' : 's-inactive'">{{ u.isActive ? '● Active' : '● Inactive' }}</span></td>
              <td class="muted">{{ u.createdAt | date:'dd MMM yyyy' }}</td>
              <td>
                <div class="action-btns">
                  <button class="btn-edit" (click)="openEdit(u)">✏️ Edit</button>
                  <button class="btn-toggle" [class]="u.isActive ? 'btn-deact' : 'btn-act'" (click)="toggleActive(u)">{{ u.isActive ? 'Deactivate' : 'Activate' }}</button>
                  <button class="btn-del" (click)="deleteUser(u)">🗑️</button>
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>
    }
  </div>
</div>

<!-- Edit Modal -->
@if (editUser()) {
  <div class="overlay" (click)="closeEdit()">
    <div class="modal" (click)="$event.stopPropagation()">
      <div class="modal-head">
        <div class="modal-title-wrap">
          <div class="modal-icon">✏️</div>
          <div><h2>Edit User</h2><p class="modal-sub">Update user information</p></div>
        </div>
        <button class="close-btn" (click)="closeEdit()">✕</button>
      </div>
      <div class="modal-body">
        <div class="form-grid">
          <div class="field-group"><label class="field-label">First Name</label><input class="field" [(ngModel)]="editForm.firstName" /></div>
          <div class="field-group"><label class="field-label">Last Name</label><input class="field" [(ngModel)]="editForm.lastName" /></div>
          <div class="field-group"><label class="field-label">Email</label><input class="field" [(ngModel)]="editForm.email" type="email" /></div>
          <div class="field-group"><label class="field-label">Phone</label><input class="field" [(ngModel)]="editForm.phoneNumber" /></div>
        </div>
        @if (editError()) { <div class="form-error">⚠️ {{ editError() }}</div> }
      </div>
      <div class="modal-foot">
        <button class="btn-cancel" (click)="closeEdit()">Cancel</button>
        <button class="btn-save" (click)="saveEdit()">💾 Save Changes</button>
      </div>
    </div>
  </div>
}
  `,
  styles: [`
    * { box-sizing:border-box; }
    .page { padding:28px; color:var(--adm-text); min-height:100vh; background:var(--adm-bg); }
    .page-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:28px; }
    .page-title { font-size:26px; font-weight:800; margin:0; background:linear-gradient(135deg,#38bdf8,#818cf8); -webkit-background-clip:text; -webkit-text-fill-color:transparent; }
    .page-sub { margin:4px 0 0; font-size:13px; color:var(--adm-text2); }
    .result-count { font-size:13px; color:var(--adm-text2); background:var(--adm-card); border:1px solid var(--adm-border); padding:6px 14px; border-radius:20px; }

    .stats { display:grid; grid-template-columns:repeat(4,1fr); gap:14px; margin-bottom:24px; }
    .stat-card { display:flex; align-items:center; gap:14px; border-radius:14px; padding:18px; border:1px solid rgba(255,255,255,.06); }
    .c1{background:var(--adm-s1);} .c2{background:var(--adm-s2);} .c3{background:var(--adm-s5);} .c4{background:var(--adm-s4);}
    .stat-icon{font-size:26px;} .stat-val{font-size:28px;font-weight:800;color:var(--adm-text);line-height:1;} .stat-lbl{font-size:12px;color:var(--adm-text3);margin-top:4px;}

    .filters { display:flex; gap:12px; margin-bottom:20px; }
    .search-wrap { flex:1; position:relative; }
    .search-icon { position:absolute; left:12px; top:50%; transform:translateY(-50%); font-size:14px; }
    .search { width:100%; background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:10px 14px 10px 36px; border-radius:8px; font-size:14px; }
    .search:focus { outline:none; border-color:#38bdf8; }
    .fsel { background:var(--adm-card); border:1px solid var(--adm-border2); color:var(--adm-text); padding:10px 14px; border-radius:8px; font-size:14px; }

    .table-card { background:var(--adm-card); border-radius:14px; border:1px solid var(--adm-border); overflow:hidden; }
    table { width:100%; border-collapse:collapse; }
    thead tr { background:var(--adm-card2); }
    th { padding:12px 16px; text-align:left; font-size:11px; color:var(--adm-text2); font-weight:700; letter-spacing:.06em; border-bottom:1px solid var(--adm-border); }
    td { padding:11px 16px; font-size:13px; border-bottom:1px solid var(--adm-border); vertical-align:middle; color:var(--adm-text); }
    tr.row-alt td { background:var(--adm-row-alt); }
    tbody tr:hover td { background:var(--adm-row-hover); }

    .user-cell { display:flex; align-items:center; gap:12px; }
    .avatar { width:38px; height:38px; border-radius:50%; display:flex; align-items:center; justify-content:center; font-size:13px; font-weight:700; color:#fff; flex-shrink:0; }
    .av-admin{background:linear-gradient(135deg,#7c3aed,#6d28d9);} .av-storemanager{background:linear-gradient(135deg,#2563eb,#1d4ed8);} .av-deliverydriver{background:linear-gradient(135deg,#d97706,#b45309);} .av-customer{background:linear-gradient(135deg,#16a34a,#15803d);}
    .uname { font-weight:600; color:var(--adm-text); margin:0; font-size:13.5px; }
    .uemail { color:var(--adm-text3); font-size:11px; margin:2px 0 0; }
    .muted { color:var(--adm-text2); font-size:12px; }

    .role-chip { padding:4px 12px; border-radius:20px; font-size:12px; font-weight:600; cursor:pointer; }
    .r-admin{background:rgba(124,58,237,.2);color:#a78bfa;border:1px solid rgba(124,58,237,.3);}
    .r-storemanager{background:rgba(37,99,235,.2);color:#60a5fa;border:1px solid rgba(37,99,235,.3);}
    .r-deliverydriver{background:rgba(217,119,6,.2);color:#fbbf24;border:1px solid rgba(217,119,6,.3);}
    .r-customer{background:rgba(22,163,74,.2);color:#4ade80;border:1px solid rgba(22,163,74,.3);}
    .role-select { background:var(--adm-card2); border:1px solid var(--adm-border2); color:var(--adm-text); padding:4px 8px; border-radius:6px; font-size:12px; }

    .status-chip { padding:4px 12px; border-radius:20px; font-size:12px; font-weight:600; }
    .s-active{background:rgba(34,197,94,.15);color:#4ade80;border:1px solid rgba(34,197,94,.3);}
    .s-inactive{background:rgba(239,68,68,.12);color:#f87171;border:1px solid rgba(239,68,68,.25);}

    .action-btns { display:flex; gap:6px; align-items:center; }
    .btn-edit { background:linear-gradient(135deg,#2563eb,#3b82f6); color:#fff; border:none; padding:6px 12px; border-radius:7px; cursor:pointer; font-size:12px; font-weight:600; transition:all .2s; }
    .btn-edit:hover { transform:translateY(-1px); box-shadow:0 4px 12px rgba(59,130,246,.4); }
    .btn-toggle { border:none; padding:6px 10px; border-radius:7px; cursor:pointer; font-size:12px; font-weight:600; transition:all .2s; }
    .btn-deact { background:rgba(251,191,36,.15); color:#fbbf24; border:1px solid rgba(251,191,36,.3); }
    .btn-act { background:rgba(34,197,94,.15); color:#4ade80; border:1px solid rgba(34,197,94,.3); }
    .btn-del { background:rgba(239,68,68,.15); color:#f87171; border:1px solid rgba(239,68,68,.25); padding:6px 10px; border-radius:7px; cursor:pointer; font-size:13px; transition:all .2s; }
    .btn-del:hover { background:rgba(239,68,68,.3); }

    .empty { text-align:center; padding:60px; display:flex; flex-direction:column; align-items:center; gap:10px; color:var(--adm-text3); }
    .empty span { font-size:40px; } .empty p { margin:0; font-size:15px; }
    .retry-btn { background:var(--adm-card); color:var(--adm-text2); border:1px solid var(--adm-border2); padding:8px 18px; border-radius:8px; cursor:pointer; font-size:13px; }
    .loading-rows { padding:16px; display:flex; flex-direction:column; gap:10px; }
    .skeleton { height:52px; background:var(--adm-card); border-radius:8px; animation:pulse 1.5s infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.5} }

    /* Modal */
    .overlay { position:fixed; inset:0; background:rgba(0,0,0,.75); backdrop-filter:blur(4px); display:flex; align-items:center; justify-content:center; z-index:1000; padding:16px; }
    .modal { background:var(--adm-card); border:1px solid var(--adm-border); border-radius:16px; width:520px; max-width:100%; box-shadow:0 25px 80px rgba(0,0,0,.6); }
    .modal-head { display:flex; justify-content:space-between; align-items:flex-start; padding:20px 24px; border-bottom:1px solid var(--adm-border); background:linear-gradient(135deg,var(--adm-card2),var(--adm-card)); border-radius:16px 16px 0 0; }
    .modal-title-wrap { display:flex; align-items:center; gap:12px; }
    .modal-icon { font-size:26px; }
    .modal-head h2 { margin:0; font-size:17px; font-weight:700; color:var(--adm-text); }
    .modal-sub { margin:3px 0 0; font-size:12px; color:var(--adm-text3); }
    .close-btn { background:rgba(255,255,255,.07); border:1px solid var(--adm-border2); color:var(--adm-text2); width:30px; height:30px; border-radius:8px; cursor:pointer; font-size:13px; display:flex; align-items:center; justify-content:center; }
    .close-btn:hover { background:rgba(239,68,68,.2); color:#f87171; }
    .modal-body { padding:22px 24px; }
    .form-grid { display:grid; grid-template-columns:1fr 1fr; gap:14px; }
    .field-group { display:flex; flex-direction:column; gap:5px; }
    .field-label { font-size:11px; font-weight:600; color:var(--adm-text2); text-transform:uppercase; letter-spacing:.05em; }
    .field { background:var(--adm-input-bg); border:1px solid var(--adm-border); color:var(--adm-text); padding:10px 13px; border-radius:8px; font-size:14px; width:100%; }
    .field:focus { outline:none; border-color:#38bdf8; box-shadow:0 0 0 3px rgba(56,189,248,.1); }
    .form-error { background:rgba(239,68,68,.1); border:1px solid rgba(239,68,68,.3); color:#f87171; padding:10px 14px; border-radius:8px; font-size:13px; margin-top:12px; }
    .modal-foot { display:flex; justify-content:flex-end; gap:12px; padding:16px 24px; border-top:1px solid var(--adm-border); background:var(--adm-card2); border-radius:0 0 16px 16px; }
    .btn-cancel { background:var(--adm-card); color:var(--adm-text2); border:1px solid var(--adm-border2); padding:10px 20px; border-radius:8px; cursor:pointer; font-size:14px; }
    .btn-cancel:hover { background:var(--adm-border); }
    .btn-save { background:linear-gradient(135deg,#22c55e,#16a34a); color:#fff; border:none; padding:10px 22px; border-radius:8px; font-weight:700; font-size:14px; cursor:pointer; box-shadow:0 4px 15px rgba(34,197,94,.3); }
    .btn-save:hover { transform:translateY(-1px); }
  `]
})
export class AdminUsers implements OnInit {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/api/v1/users`;

  users = signal<UserAdmin[]>([]);
  filtered = signal<UserAdmin[]>([]);
  stats = signal<UserStats | null>(null);
  loading = signal(true);
  error = signal('');
  search = ''; filterRole = ''; filterActive = '';
  editUser = signal<UserAdmin | null>(null);
  editForm = { firstName:'', lastName:'', email:'', phoneNumber:'' };
  editError = signal('');
  editingRole = ''; newRole = '';

  ngOnInit() { this.load(); this.http.get<UserStats>(`${this.base}/stats`).subscribe(s => this.stats.set(s)); }

  load() {
    this.loading.set(true); this.error.set('');
    this.http.get<UserAdmin[]>(this.base).subscribe({
      next: u => { this.users.set(u); this.applyFilters(); this.loading.set(false); },
      error: err => { this.error.set(`Failed to load users (${err.status})`); this.loading.set(false); }
    });
  }

  applyFilters() {
    let list = this.users();
    if (this.search) { const s = this.search.toLowerCase(); list = list.filter(u => u.email.toLowerCase().includes(s) || `${u.firstName} ${u.lastName}`.toLowerCase().includes(s)); }
    if (this.filterRole) list = list.filter(u => u.role === this.filterRole);
    if (this.filterActive !== '') list = list.filter(u => String(u.isActive) === this.filterActive);
    this.filtered.set(list);
  }

  roleCount(role: string) { return this.stats()?.byRole.find(r => r.role === role)?.count ?? 0; }
  initials(u: UserAdmin) { return ((u.firstName[0] ?? '') + (u.lastName[0] ?? '')).toUpperCase() || '?'; }
  startEditRole(u: UserAdmin) { this.editingRole = u.id; this.newRole = u.role; }

  saveRole(u: UserAdmin) {
    this.http.patch(`${this.base}/${u.id}/role`, { role: this.newRole }).subscribe({
      next: () => { this.users.update(l => l.map(x => x.id === u.id ? { ...x, role: this.newRole } : x)); this.applyFilters(); this.editingRole = ''; }
    });
  }

  toggleActive(u: UserAdmin) {
    this.http.patch(`${this.base}/${u.id}/toggle-active`, {}).subscribe({
      next: (res: any) => { this.users.update(l => l.map(x => x.id === u.id ? { ...x, isActive: res.isActive } : x)); this.applyFilters(); }
    });
  }

  deleteUser(u: UserAdmin) {
    if (!confirm(`Delete ${u.firstName} ${u.lastName}?`)) return;
    this.http.delete(`${this.base}/${u.id}`).subscribe({
      next: () => { this.users.update(l => l.filter(x => x.id !== u.id)); this.applyFilters(); }
    });
  }

  openEdit(u: UserAdmin) { this.editUser.set(u); this.editForm = { firstName: u.firstName, lastName: u.lastName, email: u.email, phoneNumber: u.phoneNumber ?? '' }; this.editError.set(''); }
  closeEdit() { this.editUser.set(null); }

  saveEdit() {
    const u = this.editUser(); if (!u) return;
    this.http.put<UserAdmin>(`${this.base}/${u.id}`, this.editForm).subscribe({
      next: updated => { this.users.update(l => l.map(x => x.id === u.id ? updated : x)); this.applyFilters(); this.closeEdit(); },
      error: err => this.editError.set(err.error?.error ?? 'Failed to update user')
    });
  }
}
