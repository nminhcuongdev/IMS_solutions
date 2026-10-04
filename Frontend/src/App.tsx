import { lazy, Suspense } from "react";
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { AuthProvider, useAuth } from "@/auth/AuthContext";
import { LanguageProvider } from "@/i18n/LanguageContext";
import { ToastProvider } from "@/lib/Toast";
import { ConfirmProvider } from "@/lib/Confirm";
import { AppLayout } from "@/layout/AppLayout";
import { LoginPage } from "@/pages/LoginPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { ZonePage } from "@/pages/ZonePage";
import { FactoryPage } from "@/pages/FactoryPage";
import { LocationPage } from "@/pages/LocationPage";
import { ItemGroupPage } from "@/pages/ItemGroupPage";
import { ItemPage } from "@/pages/ItemPage";
import { PartnerPage } from "@/pages/PartnerPage";
import { ReasonPage } from "@/pages/ReasonPage";
import { UserPage } from "@/pages/UserPage";
import { SettingsPage } from "@/pages/SettingsPage";
import { PermissionPage } from "@/pages/PermissionPage";
import { LanguageDataPage } from "@/pages/LanguageDataPage";
import { PrintHistoryPage } from "@/pages/PrintHistoryPage";
import { RfidHistoryPage } from "@/pages/RfidHistoryPage";
import { ReceivingSchedulePage } from "@/pages/ReceivingSchedulePage";
import { ReturnsPage } from "@/pages/ReturnsPage";
import { ReceivingResultPage } from "@/pages/ReceivingResultPage";
import { ShippingSchedulePage } from "@/pages/ShippingSchedulePage";
import { AlertCenterPage } from "@/pages/AlertCenterPage";
import { StocktakingPage } from "@/pages/StocktakingPage";
import { StocktakingHistoryPage } from "@/pages/StocktakingHistoryPage";
import { RfidStockCountPage } from "@/pages/RfidStockCountPage";
import { ShippingResultPage } from "@/pages/ShippingResultPage";
import { StockInquiryPage } from "@/pages/StockInquiryPage";
import { WorkHistoryPage } from "@/pages/WorkHistoryPage";
import { HoldItemPage } from "@/pages/HoldItemPage";
import { PickingSchedulePage } from "@/pages/PickingSchedulePage";
import { PickingResultPage } from "@/pages/PickingResultPage";
import { SuperAdminPage } from "@/pages/SuperAdminPage";
import { PlaceholderPage } from "@/pages/PlaceholderPage";

const Location3DPage = lazy(() =>
  import("@/pages/Location3DPage").then(m => ({ default: m.Location3DPage })));

function toLogin(loc: ReturnType<typeof useLocation>) {
  return <Navigate to="/login" replace state={{ from: loc.pathname + loc.search }} />;
}

function KeepQuery({ to }: { to: string }) {
  const { search } = useLocation();
  return <Navigate to={to + search} replace />;
}

function AuthGate() {
  return <div className="page-info p-6" />;
}

function RequireAuth({ children }: { children: React.ReactNode }) {
  const { user, booting } = useAuth();
  const loc = useLocation();
  if (booting) return <AuthGate />;
  return user ? <>{children}</> : toLogin(loc);
}
function RequireSuperAdmin({ children }: { children: React.ReactNode }) {
  const { user, booting } = useAuth();
  const loc = useLocation();
  if (booting) return <AuthGate />;
  if (!user) return toLogin(loc);
  return user.isSuperAdmin ? <>{children}</> : <Navigate to="/dashboard" replace />;
}

export default function App() {
  return (
    <AuthProvider>
      <LanguageProvider>
      <ToastProvider>
      <ConfirmProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<RequireAuth><AppLayout /></RequireAuth>}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />

          <Route path="masters/factories"   element={<FactoryPage />} />
          <Route path="masters/zones"       element={<ZonePage />} />
          <Route path="masters/locations"   element={<LocationPage />} />
          <Route path="masters/locations-3d" element={
            <Suspense fallback={<div className="page-info p-6">Đang tải khung cảnh 3D…</div>}>
              <Location3DPage />
            </Suspense>} />
          <Route path="masters/item-groups" element={<ItemGroupPage />} />
          <Route path="masters/items"       element={<ItemPage />} />
          <Route path="masters/partners"    element={<PartnerPage />} />
          <Route path="masters/reasons"     element={<ReasonPage />} />
          <Route path="masters/users"       element={<UserPage />} />


          <Route path="receiving/schedule" element={<ReceivingSchedulePage />} />
          <Route path="receiving/returns" element={<ReturnsPage />} />
          <Route path="receiving/result"   element={<ReceivingResultPage />} />

          <Route path="inventory/stock"   element={<StockInquiryPage />} />
          <Route path="inventory/history" element={<WorkHistoryPage />} />
          <Route path="inventory/hold"    element={<HoldItemPage />} />
          <Route path="picking/schedule" element={<PickingSchedulePage />} />
          <Route path="picking/result"   element={<PickingResultPage />} />

          <Route path="shipping/schedule" element={<ShippingSchedulePage />} />
          <Route path="shipping/result"   element={<ShippingResultPage />} />

          <Route path="alerts" element={<AlertCenterPage />} />

          <Route path="stocktaking/labels"  element={<StocktakingPage />} />
          <Route path="stocktaking/history" element={<StocktakingHistoryPage />} />
          <Route path="stocktaking/rfid"    element={<RfidStockCountPage />} />

          <Route path="other/permissions"   element={<PermissionPage />} />
          <Route path="other/settings"      element={<SettingsPage />} />
          <Route path="other/language-data" element={<LanguageDataPage />} />
          <Route path="history/print" element={<PrintHistoryPage />} />
          <Route path="history/rfid"  element={<RfidHistoryPage />} />
          <Route path="other/print-history" element={<KeepQuery to="/history/print" />} />
          <Route path="other/rfid-history"  element={<KeepQuery to="/history/rfid" />} />

          <Route path="other/:code"   element={<PlaceholderPage />} />
          <Route path="super-admin"   element={<RequireSuperAdmin><SuperAdminPage /></RequireSuperAdmin>} />
          <Route path="wip/:code"     element={<PlaceholderPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
      </ConfirmProvider>
      </ToastProvider>
      </LanguageProvider>
    </AuthProvider>
  );
}
