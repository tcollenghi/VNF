
// DO NOT REMOVE : GLOBAL FUNCTIONS!

$(document).ready(function () {

    pageSetUp();

    /* // DOM Position key index //

    l - Length changing (dropdown)
    f - Filtering input (search)
    t - The Table! (datatable)
    i - Information (records)
    p - Pagination (paging)
    r - pRocessing
    < and > - div elements
    <"#id" and > - div with an id
    <"class" and > - div with a class
    <"#id.class" and > - div with an id and class

    Also see: http://legacy.datatables.net/usage/features
    */

    LoadData();

    /* BASIC ;*/
    var responsiveHelper_dt_basic = undefined;
    var responsiveHelper_datatable_fixed_column = undefined;
    var responsiveHelper_datatable_col_reorder = undefined;
    var responsiveHelper_datatable_tabletools = undefined;

    var breakpointDefinition = {
        tablet: 1024,
        phone: 480
    };

    /* COLUMN SHOW - HIDE */
    $('#dt_basic').dataTable({
        "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-12 hidden-xs'l>r>" +
                         "t" +
                         "<'dt-toolbar-footer'<'col-sm-6 col-xs-12 hidden-xs'i><'col-xs-12 col-sm-6'p>>",
        "autoWidth": true,
        "preDrawCallback": function () {
            // Initialize the responsive datatables helper once.
            if (!responsiveHelper_dt_basic) {
                responsiveHelper_dt_basic = new ResponsiveDatatablesHelper($('#dt_basic'), breakpointDefinition);
            }
        },
        "rowCallback": function (nRow) {
            responsiveHelper_dt_basic.createExpandIcon(nRow);
        },
        "drawCallback": function (oSettings) {
            responsiveHelper_dt_basic.respond();
        }
    });

    
})

$(".table-click tr").on('click', function (e) {
    $(this).siblings().removeClass("selecionado");
    $(this).toggleClass("selecionado");
    
});

$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});

function LoadData() {
    $.ajax({
        url: url + 'GridPadrao/GetData',
        async: false,
        cache: false,
        type: 'GET',
        dataType: 'json',
        error: function () {
        },
        success: function (data) {
            $("#datatable_col_reorder tbody").empty();
        }
    });
}

// With Callback
$("#deletelinha").click(function (e) {
    $.SmartMessageBox({
        title: "Alerta de Exclusão",
        content: "Tem certeza que deseja excluir este(s) item(s)",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {

            $.smallBox({
                title: "Sucesso",
                content: "<i class='fa fa-clock-o'></i> <i>Item(s) excluido(s) com sucesso</i>",
                color: "#739E73",
                iconSmall: "fa fa-check fa-2x fadeInRight animated",
                timeout: 4000
            });

            $("tr.selecionado").remove();
        }
        if (ButtonPressed === "Não") {
            $.smallBox({
                title: "Cancelado",
                content: "<i class='fa fa-clock-o'></i> <i>Item(s) não excluido(s)</i>",
                color: "#C46A69",
                iconSmall: "fa fa-times fa-2x fadeInRight animated",
                timeout: 4000
            });
        }

    });
    e.preventDefault();
})
