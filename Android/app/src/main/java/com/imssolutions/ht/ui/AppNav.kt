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

object Routes {
    const val LOGIN = "login"
    const val SETTINGS = "settings"
    const val MENU = "menu"
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
                onLoggedOut = { nav.navigate(Routes.LOGIN) { popUpTo(0) } },
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
    }
}
