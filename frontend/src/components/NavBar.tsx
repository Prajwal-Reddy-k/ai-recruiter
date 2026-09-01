import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Bell, ChevronDown, Download, LayoutDashboard, LogOut, Menu, MessageSquare, Moon, Settings, Sun, User, X } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { useTheme } from "../context/ThemeContext";
import { getExternalJobsAvailability } from "../api/externalJobs";
import { getMyNotifications, getUnreadCount, markNotificationRead } from "../api/notifications";
import { getUnreadMessageCount } from "../api/messages";
import type { AppNotification } from "../types";
import { resolveAvatarUrl } from "../utils/format";
import Avatar from "./ui/Avatar";

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
}

export default function NavBar() {
  const { user, isAuthenticated, logout } = useAuth();
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();
  const [externalJobsAvailable, setExternalJobsAvailable] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [notifOpen, setNotifOpen] = useState(false);
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [unreadMessages, setUnreadMessages] = useState(0);
  const [installPrompt, setInstallPrompt] = useState<BeforeInstallPromptEvent | null>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const notifRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleBeforeInstallPrompt(e: Event) {
      e.preventDefault();
      setInstallPrompt(e as BeforeInstallPromptEvent);
    }
    window.addEventListener("beforeinstallprompt", handleBeforeInstallPrompt);
    return () => window.removeEventListener("beforeinstallprompt", handleBeforeInstallPrompt);
  }, []);

  async function handleInstallClick() {
    if (!installPrompt) return;
    await installPrompt.prompt();
    setInstallPrompt(null);
  }

  useEffect(() => {
    if (isAuthenticated) {
      getExternalJobsAvailability()
        .then(setExternalJobsAvailable)
        .catch(() => setExternalJobsAvailable(false));
      getUnreadCount()
        .then(setUnreadCount)
        .catch(() => setUnreadCount(0));
      if (user?.role === "Candidate" || user?.role === "Recruiter") {
        getUnreadMessageCount()
          .then(setUnreadMessages)
          .catch(() => setUnreadMessages(0));
      }
    }
  }, [isAuthenticated, user?.role]);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setMenuOpen(false);
      }
      if (notifRef.current && !notifRef.current.contains(e.target as Node)) {
        setNotifOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  function handleLogout() {
    setMenuOpen(false);
    setMobileOpen(false);
    logout();
    navigate("/login");
  }

  async function toggleNotifications() {
    const next = !notifOpen;
    setNotifOpen(next);
    if (next) {
      try {
        const data = await getMyNotifications();
        setNotifications(data);
      } catch {
        setNotifications([]);
      }
    }
  }

  async function handleNotificationClick(notif: AppNotification) {
    if (!notif.isRead) {
      try {
        await markNotificationRead(notif.id);
        setNotifications((prev) => prev.map((n) => (n.id === notif.id ? { ...n, isRead: true } : n)));
        setUnreadCount((c) => Math.max(0, c - 1));
      } catch {
        // best-effort — leave state as-is on failure
      }
    }
  }

  const dashboardPath = user?.role === "Recruiter" ? "/recruiter/dashboard" : user?.role === "Admin" ? "/admin" : "/candidate/dashboard";
  const profilePath = user?.role === "Recruiter" ? "/onboarding" : "/profile";
  const profileLabel = user?.role === "Recruiter" ? "Company Profile" : "Edit Profile";

  const roleLinks =
    user?.role === "Recruiter" ? (
      <>
        <Link to="/recruiter/dashboard" onClick={() => setMobileOpen(false)}>Dashboard</Link>
        <Link to="/onboarding" onClick={() => setMobileOpen(false)}>Company Setup</Link>
        <Link to="/jobs/mine" onClick={() => setMobileOpen(false)}>Manage Jobs</Link>
        <Link to="/post-job" onClick={() => setMobileOpen(false)}>Post a Job</Link>
        <Link to="/recruiter/candidates" onClick={() => setMobileOpen(false)}>Candidates</Link>
        <Link to="/recruiter/interviews" onClick={() => setMobileOpen(false)}>Interviews</Link>
        <Link to="/recruiter/templates" onClick={() => setMobileOpen(false)}>Templates</Link>
        <Link to="/recruiter/messages" onClick={() => setMobileOpen(false)}>Messages</Link>
        <Link to="/recruiter/team" onClick={() => setMobileOpen(false)}>Team</Link>
        <Link to="/recruiter/analytics" onClick={() => setMobileOpen(false)}>Analytics</Link>
        <Link to="/recruiter/reports" onClick={() => setMobileOpen(false)}>Reports</Link>
      </>
    ) : user?.role === "Candidate" ? (
      <>
        <Link to="/candidate/dashboard" onClick={() => setMobileOpen(false)}>Dashboard</Link>
        <Link to="/profile" onClick={() => setMobileOpen(false)}>My Profile</Link>
        <Link to="/resume-builder" onClick={() => setMobileOpen(false)}>Resume Builder</Link>
        <Link to="/applications" onClick={() => setMobileOpen(false)}>My Applications</Link>
        <Link to="/interviews" onClick={() => setMobileOpen(false)}>Interviews</Link>
        <Link to="/messages" onClick={() => setMobileOpen(false)}>Messages</Link>
        <Link to="/saved-jobs" onClick={() => setMobileOpen(false)}>Saved Jobs</Link>
        <Link to="/alerts" onClick={() => setMobileOpen(false)}>Job Alerts</Link>
      </>
    ) : user?.role === "Admin" ? (
      <Link to="/admin" onClick={() => setMobileOpen(false)}>Admin</Link>
    ) : null;

  return (
    <header className="navbar">
      <div className="navbar-inner">
        <Link to="/jobs" className="brand-mark" onClick={() => setMobileOpen(false)}>
          <span className="brand-ai">AI</span> Recruiter
        </Link>

        <nav className="nav-links nav-links-desktop" aria-label="Primary">
          <Link to="/jobs">Jobs</Link>
          {externalJobsAvailable && <Link to="/external-jobs">External Jobs</Link>}
          {isAuthenticated && roleLinks}
        </nav>

        <div className="navbar-actions">
          {isAuthenticated ? (
            <>
              {(user?.role === "Candidate" || user?.role === "Recruiter") && (
                <Link
                  to={user.role === "Recruiter" ? "/recruiter/messages" : "/messages"}
                  className="icon-btn"
                  aria-label={`Messages${unreadMessages > 0 ? ` (${unreadMessages} unread)` : ""}`}
                  onClick={() => setMobileOpen(false)}
                >
                  <MessageSquare size={19} />
                  {unreadMessages > 0 && <span className="notif-badge">{unreadMessages > 9 ? "9+" : unreadMessages}</span>}
                </Link>
              )}
              <div className="user-menu" ref={notifRef}>
                <button
                  type="button"
                  className="icon-btn"
                  aria-label={`Notifications${unreadCount > 0 ? ` (${unreadCount} unread)` : ""}`}
                  onClick={toggleNotifications}
                  aria-expanded={notifOpen}
                  aria-haspopup="menu"
                >
                  <Bell size={19} />
                  {unreadCount > 0 && <span className="notif-badge">{unreadCount > 9 ? "9+" : unreadCount}</span>}
                </button>
                {notifOpen && (
                  <div className="user-menu-dropdown notif-dropdown" role="menu">
                    {notifications.length === 0 ? (
                      <p className="hint" style={{ padding: "0.75rem" }}>No notifications yet.</p>
                    ) : (
                      notifications.slice(0, 8).map((n) => (
                        <button
                          key={n.id}
                          type="button"
                          className={`notif-item ${n.isRead ? "" : "notif-item-unread"}`}
                          onClick={() => handleNotificationClick(n)}
                        >
                          <span>{n.message}</span>
                          <span className="hint">{new Date(n.createdAt).toLocaleString()}</span>
                        </button>
                      ))
                    )}
                  </div>
                )}
              </div>
              <div className="user-menu" ref={menuRef}>
                <button
                  type="button"
                  className="user-menu-trigger"
                  onClick={() => setMenuOpen((v) => !v)}
                  aria-expanded={menuOpen}
                  aria-haspopup="menu"
                >
                  <Avatar name={user?.fullName ?? "?"} size={32} src={resolveAvatarUrl(user?.avatarUrl)} />
                  <span className="user-menu-name">{user?.fullName}</span>
                  <ChevronDown size={16} />
                </button>
                {menuOpen && (
                  <div className="user-menu-dropdown" role="menu">
                    <Link to={dashboardPath} role="menuitem" onClick={() => setMenuOpen(false)}>
                      <LayoutDashboard size={16} /> Dashboard
                    </Link>
                    {user?.role !== "Admin" && (
                      <Link to={profilePath} role="menuitem" onClick={() => setMenuOpen(false)}>
                        <User size={16} /> {profileLabel}
                      </Link>
                    )}
                    <Link to="/settings" role="menuitem" onClick={() => setMenuOpen(false)}>
                      <Settings size={16} /> Account Settings
                    </Link>
                    <button type="button" role="menuitem" onClick={handleLogout}>
                      <LogOut size={16} /> Logout
                    </button>
                  </div>
                )}
              </div>
            </>
          ) : (
            <div className="navbar-auth-links">
              <Link to="/login" className="btn btn-ghost btn-sm">Login</Link>
              <Link to="/register" className="btn btn-primary btn-sm">Register</Link>
            </div>
          )}

          <button
            type="button"
            className="icon-btn theme-toggle-btn"
            aria-label={theme === "dark" ? "Switch to light theme" : "Switch to dark theme"}
            title={theme === "dark" ? "Switch to light theme" : "Switch to dark theme"}
            onClick={toggleTheme}
          >
            {theme === "dark" ? <Sun size={19} /> : <Moon size={19} />}
          </button>

          {installPrompt && (
            <button
              type="button"
              className="icon-btn"
              aria-label="Install AI Recruiter"
              title="Install AI Recruiter"
              onClick={handleInstallClick}
            >
              <Download size={19} />
            </button>
          )}

          <button
            type="button"
            className="icon-btn nav-mobile-toggle"
            onClick={() => setMobileOpen((v) => !v)}
            aria-label={mobileOpen ? "Close menu" : "Open menu"}
            aria-expanded={mobileOpen}
          >
            {mobileOpen ? <X size={22} /> : <Menu size={22} />}
          </button>
        </div>
      </div>

      {mobileOpen && (
        <nav className="nav-links-mobile" aria-label="Mobile">
          <Link to="/jobs" onClick={() => setMobileOpen(false)}>Jobs</Link>
          {externalJobsAvailable && <Link to="/external-jobs" onClick={() => setMobileOpen(false)}>External Jobs</Link>}
          {isAuthenticated && roleLinks}
          {isAuthenticated ? (
            <button type="button" onClick={handleLogout} className="btn btn-secondary btn-sm">
              Logout
            </button>
          ) : (
            <>
              <Link to="/login" onClick={() => setMobileOpen(false)}>Login</Link>
              <Link to="/register" onClick={() => setMobileOpen(false)}>Register</Link>
            </>
          )}
        </nav>
      )}
    </header>
  );
}
