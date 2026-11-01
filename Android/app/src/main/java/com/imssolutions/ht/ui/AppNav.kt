package com.imssolutions.ht.ui

import android.net.Uri
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.imssolutions.ht.ui.screen.LoginScreen
import com.imssolutions.ht.ui.screen.MenuScreen
import com.imssolutions.ht.ui.screen.PickingDetailScreen
import com.imssolutions.ht.ui.screen.PickingListScreen
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.collectAsState
import com.imssolutions.ht.ui.vm.LanguageViewModel
import com.imssolutions.ht.ui.vm.DisposalViewModel
import com.imssolutions.ht.ui.vm.StocktakingViewModel
import com.imssolutions.ht.ui.vm.TransferViewModel
import com.imssolutions.ht.ui.screen.TransferPickScreen
import com.imssolutions.ht.ui.screen.TransferScanScreen
import com.imssolutions.ht.ui.screen.StocktakingLocationScreen
import com.imssolutions.ht.ui.screen.StocktakingListScreen
import com.imssolutions.ht.ui.screen.StocktakingDetailScreen
import com.imssolutions.ht.ui.screen.DisposalConfirmScreen
import com.imssolutions.ht.ui.screen.DisposalScanScreen
import com.imssolutions.ht.ui.screen.StockDetailScreen
import com.imssolutions.ht.ui.screen.StockListScreen
import com.imssolutions.ht.ui.screen.ReceivingDetailScreen
import com.imssolutions.ht.ui.screen.ReceivingItemsScreen
import com.imssolutions.ht.ui.screen.ReceivingListScreen
import com.imssolutions.ht.ui.screen.ReturnDetailScreen
import com.imssolutions.ht.ui.screen.ReturnItemsScreen
import com.imssolutions.ht.ui.screen.ReturnListScreen
import com.imssolutions.ht.ui.screen.SettingsScreen
import com.imssolutions.ht.ui.screen.ShippingDetailScreen
import com.imssolutions.ht.ui.screen.ShippingItemsScreen
import com.imssolutions.ht.ui.screen.ShippingListScreen
import com.imssolutions.ht.ui.screen.PrdConsScreen
import com.imssolutions.ht.ui.screen.PrdLabelScreen
import com.imssolutions.ht.ui.screen.PrdLinesScreen
import com.imssolutions.ht.ui.screen.PrdOrderListScreen
import com.imssolutions.ht.ui.screen.PrdOutputScreen
import com.imssolutions.ht.ui.screen.PrdReceiptScreen
import com.imssolutions.ht.ui.screen.PrdReturnScreen
import com.imssolutions.ht.ui.screen.PrdScanMaterialScreen
import com.imssolutions.ht.ui.screen.PrdScanProcessScreen
import com.imssolutions.ht.ui.vm.PrdMaterial
import com.imssolutions.ht.ui.vm.UpdateViewModel
import com.imssolutions.ht.ui.screen.UpdateHost

object Routes {
    const val LOGIN = "login"
    const val SETTINGS = "settings"
    const val MENU = "menu"
    const val PRD_LIST = "prd_list/{kind}"
    const val PRD_RCV = "prd_rcv/{no}"
    const val PRD_LINES = "prd_lines/{no}"
    const val PRD_SCAN_PROC = "prd_scan_proc/{no}"
    const val PRD_SCAN_MAT = "prd_scan_mat/{no}/{proc}"
    const val PRD_CONS = "prd_cons/{no}/{proc}/{item}?lot={lot}"
    const val PRD_RET = "prd_ret/{no}"
    const val PRD_OUT = "prd_out/{no}"
    const val PRD_LABEL = "prd_label/{no}/{req}"
    fun prdList(kind: String) = "prd_list/$kind"
    fun prdOpen(kind: String, no: String) = when (kind) {
        "receipt" -> "prd_rcv/${Uri.encode(no)}"
        "input" -> "prd_lines/${Uri.encode(no)}"
        "return" -> "prd_ret/${Uri.encode(no)}"
        else -> "prd_out/${Uri.encode(no)}"
    }
    fun prdScanProc(no: String) = "prd_scan_proc/${Uri.encode(no)}"
    fun prdScanMat(no: String, proc: String) = "prd_scan_mat/${Uri.encode(no)}/${Uri.encode(proc)}"
    fun prdCons(no: String, proc: String, m: PrdMaterial) =
        "prd_cons/${Uri.encode(no)}/${Uri.encode(proc)}/${Uri.encode(m.itemCode)}?lot=${Uri.encode(m.lotNo)}"
    fun prdLabel(no: String, req: String) = "prd_label/${Uri.encode(no)}/${Uri.encode(req)}"
    const val RECEIVING = "rcv_list"
    const val ITEMS = "rcv_items/{poNo}"
    const val DETAIL = "rcv_detail/{poNo}/{serialNo}?qty={qty}&lot={lot}"
    fun items(poNo: String) = "rcv_items/$poNo"
    fun detail(poNo: String, serialNo: String, qty: String? = null, lot: String? = null) =
        "rcv_detail/$poNo/$serialNo?qty=${Uri.encode(qty.orEmpty())}&lot=${Uri.encode(lot.orEmpty())}"

    const val RETURNS = "rtn_list"
    const val RTN_ITEMS = "rtn_items/{returnNo}"
    const val RTN_DETAIL = "rtn_detail/{returnNo}/{serialNo}?qty={qty}&lot={lot}"
    fun rtnItems(returnNo: String) = "rtn_items/$returnNo"
    fun rtnDetail(returnNo: String, serialNo: String, qty: String? = null, lot: String? = null) =
        "rtn_detail/$returnNo/$serialNo?qty=${Uri.encode(qty.orEmpty())}&lot=${Uri.encode(lot.orEmpty())}"

    const val PICKING = "pick_list"
    const val PICK_DETAIL = "pick_detail/{soNo}/{serialNo}"
    fun pickDetail(soNo: String, serialNo: String) = "pick_detail/$soNo/$serialNo"

    const val SHIPPING = "shp_list"
    const val SHP_ITEMS = "shp_items/{slipNo}"
    const val SHP_DETAIL = "shp_detail/{slipNo}/{serialNo}?qty={qty}&lot={lot}"
    fun shpItems(slipNo: String) = "shp_items/$slipNo"
    fun shpDetail(slipNo: String, serialNo: String, qty: String? = null, lot: String? = null) =
        "shp_detail/$slipNo/$serialNo?qty=${Uri.encode(qty.orEmpty())}&lot=${Uri.encode(lot.orEmpty())}"

    const val INVENTORY = "inv_list"
    const val INV_DETAIL = "inv_detail/{itemCode}"
    fun invDetail(itemCode: String) = "inv_detail/${Uri.encode(itemCode)}"

    const val DISPOSAL = "disp_scan"
    const val DISP_CONFIRM = "disp_confirm"

    const val TRANSFER = "transfer"
    const val TRANSFER_SCAN = "transfer_scan"

    const val STOCKTAKING = "st_label_loc"
    const val ST_LIST = "st_label_list"
    const val ST_DETAIL = "st_label_detail"
}

@Composable
fun AppNav(loggedIn: Boolean) {
    val nav = rememberNavController()

    val langVm: LanguageViewModel = viewModel()
    val strings by langVm.state.collectAsState()
    val lang = strings.lang
    val onLang: (HtLang) -> Unit = langVm::pick
    val updVm: UpdateViewModel = viewModel()
    val updState by updVm.state.collectAsState()
    val pendingUpdate = updState.offer?.entry?.versionName

    CompositionLocalProvider(LocalHtStrings provides strings) {
    NavHost(
        navController = nav,
        startDestination = if (loggedIn) Routes.MENU else Routes.LOGIN,
    ) {
        composable(Routes.LOGIN) {
            LoginScreen(
                lang = lang,
                onLang = onLang,
                onOpenSettings = { nav.navigate(Routes.SETTINGS) },
                onLoggedIn = {
                    nav.navigate(Routes.MENU) {
                        popUpTo(Routes.LOGIN) { inclusive = true }
                    }
                },
            )
        }
        composable(Routes.SETTINGS) {
            SettingsScreen(
                lang = lang, onLang = onLang, onBack = { nav.popBackStack() },
                onDownloadLabels = langVm::download,
                onClearLabels = langVm::clear,
                installedVersion = updVm.installedName,
                checking = updState.checking,
                onCheckUpdate = { updVm.check(manual = true) },
            )
        }
        composable(Routes.MENU) {
            MenuScreen(
                lang = lang,
                onLang = onLang,
                onReceiving = { nav.navigate(Routes.RECEIVING) },
                onReturns = { nav.navigate(Routes.RETURNS) },
                onPicking = { nav.navigate(Routes.PICKING) },
                onShipping = { nav.navigate(Routes.SHIPPING) },
                onInventory = { nav.navigate(Routes.INVENTORY) },
                onDisposal = { nav.navigate(Routes.DISPOSAL) },
                onTransfer = { nav.navigate(Routes.TRANSFER) },
                onStocktaking = { nav.navigate(Routes.STOCKTAKING) },
                onProduction = { kind -> nav.navigate(Routes.prdList(kind)) },
                onLoggedOut = { nav.navigate(Routes.LOGIN) { popUpTo(0) } },
                pendingUpdate = pendingUpdate,
                onOpenUpdate = updVm::open,
            )
        }
        composable(Routes.PRD_LIST) { e ->
            val kind = e.arguments?.getString("kind").orEmpty()
            PrdOrderListScreen(
                kind = kind, lang = lang, onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { no -> nav.navigate(Routes.prdOpen(kind, no)) },
            )
        }
        composable(Routes.PRD_RCV) { e ->
            PrdReceiptScreen(
                orderNo = e.arguments?.getString("no").orEmpty(), lang = lang, onLang = onLang,
                onBack = { nav.popBackStack() },
            )
        }
        composable(Routes.PRD_LINES) { e ->
            val no = e.arguments?.getString("no").orEmpty()
            PrdLinesScreen(
                orderNo = no, lang = lang, onLang = onLang,
                onBack = { nav.popBackStack() },
                onScanProcess = { nav.navigate(Routes.prdScanProc(no)) },
            )
        }
        composable(Routes.PRD_SCAN_PROC) { e ->
            val no = e.arguments?.getString("no").orEmpty()
            PrdScanProcessScreen(
                orderNo = no, lang = lang, onLang = onLang,
                onBack = { nav.popBackStack() },
                onAccepted = { proc ->
                    nav.navigate(Routes.prdScanMat(no, proc)) { popUpTo(Routes.PRD_SCAN_PROC) { inclusive = true } }
                },
            )
        }
        composable(Routes.PRD_SCAN_MAT) { e ->
            val no = e.arguments?.getString("no").orEmpty()
            val proc = e.arguments?.getString("proc").orEmpty()
            PrdScanMaterialScreen(
                orderNo = no, processCode = proc, lang = lang, onLang = onLang,
                onBack = { nav.popBackStack() },
                onBackToLines = { nav.popBackStack() },
                onAccepted = { m -> nav.navigate(Routes.prdCons(no, proc, m)) },
            )
        }
        composable(
            Routes.PRD_CONS,
            arguments = listOf(navArgument("lot") { type = NavType.StringType; defaultValue = "" }),
        ) { e ->
            PrdConsScreen(
                orderNo = e.arguments?.getString("no").orEmpty(),
                processCode = e.arguments?.getString("proc").orEmpty(),
                material = PrdMaterial(e.arguments?.getString("item").orEmpty(), e.arguments?.getString("lot").orEmpty()),
                lang = lang, onLang = onLang,
                onBack = { nav.popBackStack() },
            )
        }
        composable(Routes.PRD_RET) { e ->
            PrdReturnScreen(
                orderNo = e.arguments?.getString("no").orEmpty(), lang = lang, onLang = onLang,
                onBack = { nav.popBackStack() },
            )
        }
        composable(Routes.PRD_OUT) { e ->
            val no = e.arguments?.getString("no").orEmpty()
            PrdOutputScreen(
                orderNo = no, lang = lang, onLang = onLang,
                onBack = { nav.popBackStack() },
                onDone = { req -> nav.navigate(Routes.prdLabel(no, req)) { popUpTo(Routes.PRD_OUT) { inclusive = true } } },
            )
        }
        composable(Routes.PRD_LABEL) { e ->
            PrdLabelScreen(
                orderNo = e.arguments?.getString("no").orEmpty(),
                requestId = e.arguments?.getString("req").orEmpty(),
                lang = lang, onLang = onLang,
                onBack = { nav.popBackStack() },
            )
        }
        composable(Routes.RECEIVING) {
            ReceivingListScreen(
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { poNo -> nav.navigate(Routes.items(poNo)) },
            )
        }
        composable(Routes.ITEMS) { backStack ->
            ReceivingItemsScreen(
                poNo = backStack.arguments?.getString("poNo").orEmpty(),
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { po, serial, qty, lot -> nav.navigate(Routes.detail(po, serial, qty, lot)) },
            )
        }
        composable(Routes.RETURNS) {
            ReturnListScreen(
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { returnNo -> nav.navigate(Routes.rtnItems(returnNo)) },
            )
        }
        composable(Routes.RTN_ITEMS) { backStack ->
            ReturnItemsScreen(
                returnNo = backStack.arguments?.getString("returnNo").orEmpty(),
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { rtn, serial, qty, lot -> nav.navigate(Routes.rtnDetail(rtn, serial, qty, lot)) },
            )
        }
        composable(Routes.DISPOSAL) { entry ->
            val vm: DisposalViewModel = viewModel(entry)
            DisposalScanScreen(
                vm = vm,
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onNext = { nav.navigate(Routes.DISP_CONFIRM) },
            )
        }
        composable(Routes.DISP_CONFIRM) { entry ->
            val owner = remember(entry) { nav.getBackStackEntry(Routes.DISPOSAL) }
            val vm: DisposalViewModel = viewModel(owner)
            DisposalConfirmScreen(
                vm = vm,
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onDone = { nav.popBackStack(Routes.MENU, inclusive = false) },
            )
        }
        composable(Routes.TRANSFER) { entry ->
            val vm: TransferViewModel = viewModel(entry)
            TransferPickScreen(
                vm = vm,
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onNext = { nav.navigate(Routes.TRANSFER_SCAN) },
            )
        }
        composable(Routes.TRANSFER_SCAN) { entry ->
            val owner = remember(entry) { nav.getBackStackEntry(Routes.TRANSFER) }
            val vm: TransferViewModel = viewModel(owner)
            TransferScanScreen(
                vm = vm,
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onDone = { nav.popBackStack() },
            )
        }
        composable(Routes.STOCKTAKING) { entry ->
            val vm: StocktakingViewModel = viewModel(entry)
            StocktakingLocationScreen(
                vm = vm,
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onNext = { nav.navigate(Routes.ST_LIST) },
            )
        }
        composable(Routes.ST_LIST) { entry ->
            val owner = remember(entry) { nav.getBackStackEntry(Routes.STOCKTAKING) }
            val vm: StocktakingViewModel = viewModel(owner)
            StocktakingListScreen(
                vm = vm,
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { nav.navigate(Routes.ST_DETAIL) },
            )
        }
        composable(Routes.ST_DETAIL) { entry ->
            val owner = remember(entry) { nav.getBackStackEntry(Routes.STOCKTAKING) }
            val vm: StocktakingViewModel = viewModel(owner)
            StocktakingDetailScreen(
                vm = vm,
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
            )
        }
        composable(Routes.INVENTORY) {
            StockListScreen(
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { itemCode -> nav.navigate(Routes.invDetail(itemCode)) },
            )
        }
        composable(Routes.INV_DETAIL) { backStack ->
            StockDetailScreen(
                itemCode = backStack.arguments?.getString("itemCode").orEmpty(),
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
            )
        }
        composable(Routes.PICKING) {
            PickingListScreen(
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { soNo, serialNo -> nav.navigate(Routes.pickDetail(soNo, serialNo)) },
            )
        }
        composable(Routes.PICK_DETAIL) { backStack ->
            PickingDetailScreen(
                soNo = backStack.arguments?.getString("soNo").orEmpty(),
                serialNo = backStack.arguments?.getString("serialNo").orEmpty(),
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onDone = { nav.popBackStack() },
            )
        }
        composable(Routes.SHIPPING) {
            ShippingListScreen(
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { slipNo -> nav.navigate(Routes.shpItems(slipNo)) },
            )
        }
        composable(Routes.SHP_ITEMS) { backStack ->
            ShippingItemsScreen(
                slipNo = backStack.arguments?.getString("slipNo").orEmpty(),
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onOpen = { slip, serial, qty, lot -> nav.navigate(Routes.shpDetail(slip, serial, qty, lot)) },
            )
        }
        composable(
            Routes.SHP_DETAIL,
            arguments = listOf(
                navArgument("qty") { type = NavType.StringType; defaultValue = "" },
                navArgument("lot") { type = NavType.StringType; defaultValue = "" },
            ),
        ) { backStack ->
            ShippingDetailScreen(
                slipNo = backStack.arguments?.getString("slipNo").orEmpty(),
                serialNo = backStack.arguments?.getString("serialNo").orEmpty(),
                scannedQty = backStack.arguments?.getString("qty").orEmpty(),
                scannedLot = backStack.arguments?.getString("lot").orEmpty(),
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onDone = { nav.popBackStack() },
                onOpenSettings = { nav.navigate(Routes.SETTINGS) },
            )
        }
        composable(
            Routes.RTN_DETAIL,
            arguments = listOf(
                navArgument("qty") { type = NavType.StringType; defaultValue = "" },
                navArgument("lot") { type = NavType.StringType; defaultValue = "" },
            ),
        ) { backStack ->
            ReturnDetailScreen(
                returnNo = backStack.arguments?.getString("returnNo").orEmpty(),
                serialNo = backStack.arguments?.getString("serialNo").orEmpty(),
                scannedQty = backStack.arguments?.getString("qty").orEmpty(),
                scannedLot = backStack.arguments?.getString("lot").orEmpty(),
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onDone = { nav.popBackStack() },
                onOpenSettings = { nav.navigate(Routes.SETTINGS) },
            )
        }
        composable(
            Routes.DETAIL,
            arguments = listOf(
                navArgument("qty") { type = NavType.StringType; defaultValue = "" },
                navArgument("lot") { type = NavType.StringType; defaultValue = "" },
            ),
        ) { backStack ->
            ReceivingDetailScreen(
                poNo = backStack.arguments?.getString("poNo").orEmpty(),
                serialNo = backStack.arguments?.getString("serialNo").orEmpty(),
                scannedQty = backStack.arguments?.getString("qty").orEmpty(),
                scannedLot = backStack.arguments?.getString("lot").orEmpty(),
                lang = lang,
                onLang = onLang,
                onBack = { nav.popBackStack() },
                onDone = { nav.popBackStack() },
                onOpenSettings = { nav.navigate(Routes.SETTINGS) },
            )
        }
    }
    UpdateHost(updVm)
    }
}
