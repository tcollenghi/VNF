$(document).ready(function () {

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
        "bPaginate": true,
        "bInfo": true,
        "bScrollInfinite": true,
        "bScrollCollapse": true,
        "sScrollY": "200px",
        "bSort": false,

        "preDrawCallback": function () {
            // Initialize the responsive datatables helper once.
            if (!responsiveHelper_datatable_col_reorder) {
                responsiveHelper_datatable_col_reorder = new ResponsiveDatatablesHelper($('#dttTabela'), breakpointDefinition);
            }
        },
        "rowCallback": function (nRow) {
            responsiveHelper_datatable_col_reorder.createExpandIcon(nRow);
        },
        "drawCallback": function (oSettings) {
            responsiveHelper_datatable_col_reorder.respond();
        }
    });

    /* END COLUMN SHOW - HIDE */

   


    //$(window).scroll(function () {
    //    if ($(window).scrollTop() >= 70) {
    //        $('.navigation-wrap').addClass('fixed');
    //    }
    //    else {
    //        $('.navigation-wrap').removeClass('fixed');
    //    }
    //    if ($(window).scrollTop() >= 108) {
    //        $('.dt-toolbar').addClass('toolfixed');
    //    }
    //    else {
    //        $('.dt-toolbar').removeClass('toolfixed');
    //    }
    //    if ($(window).scrollTop() >= 150) {
    //        $('.dataTables_scrollHeadInner').addClass('theadfixed');
    //    }
    //    else {
    //        $('.dataTables_scrollHeadInner').removeClass('theadfixed');
    //    }
    //});
})

function Atualizar() {
    window.location = url + 'Compradores/Index';
}

function ExportarNF() {

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compradores/ExportarNF",
        dataType: 'html',
        data: {

        },
        error: function (erro) {

            alert(erro);
        },
        success: function (oReturn) {


            window.location = url + 'files/export/' + oReturn;
        }
    });
}

