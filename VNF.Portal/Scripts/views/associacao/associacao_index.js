
function ConsultarPedido(controller) {

    window.location = url + controller + '/ConsultarPedido/' + 4500907518;

}


function selectRow(id)
{
    $("#row" + id).siblings().removeClass("selecionado");
    $("#row" + id).toggleClass("selecionado");
    $("#hdfId").val(id);
}

function Edit(controller) {
    if ($("#hdfId").val() != "")
        window.location = url + controller + '/Edit/' + $("#hdfId").val();
}

$(document).ready(function () {
    pageSetUp();

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
    $('#dttTabela').dataTable({
        "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-12 hidden-xs'l>r>" +
						"t" +
						"<'dt-toolbar-footer'<'col-sm-6 col-xs-12 hidden-xs'i><'col-xs-12 col-sm-6'p>>",

        "autoWidth": true,
        "preDrawCallback": function () {
            // Initialize the responsive datatables helper once.
            if (!responsiveHelper_dt_basic) {
                responsiveHelper_dt_basic = new ResponsiveDatatablesHelper($('#dttTabela'), breakpointDefinition);
            }
        },
        "rowCallback": function (nRow) {
            responsiveHelper_dt_basic.createExpandIcon(nRow);
        },
        "drawCallback": function (oSettings) {
            responsiveHelper_dt_basic.respond();
        }
    });

    /* END COLUMN SHOW - HIDE */
})

function Atualizar()
{
    window.location = url +  'associacao/Index';
}

function Exportar() {
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Associacao/Exportar",
        dataType: 'html',
        error: function () {
        },
        success: function (oReturn) {
            window.location = url + 'files/export/' + oReturn;
        }
    });
}

