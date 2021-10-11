
function setTable() {
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
        "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-6 hidden-xs'C>r>" + "t",
        "bPaginate": true,
        "bInfo": false,
        "bScrollInfinite": true,
        "bScrollCollapse": true,
        "sScrollY": "200px",

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

    $(document).ready(function () {
       // var table = $('#dttTabela').dataTable();

    });


    $(window).scroll(function () {
        if ($(window).scrollTop() >= 70) {
            $('.navigation-wrap').addClass('fixed');
        }
        else {
            $('.navigation-wrap').removeClass('fixed');
        }
        if ($(window).scrollTop() >= 108) {
            $('.dt-toolbar').addClass('toolfixed');
        }
        else {
            $('.dt-toolbar').removeClass('toolfixed');
        }
        if ($(window).scrollTop() >= 150) {
            $('.dataTables_scrollHeadInner').addClass('theadfixed');
        }
        else {
            $('.dataTables_scrollHeadInner').removeClass('theadfixed');
        }
    });
}

$(document).ready(function () {
    var rows = document.getElementById("dttTabela").getElementsByTagName("tbody")[0].getElementsByTagName("tr").length;
    if (rows > 0) {
        $("#divSelecionarArquivo").addClass("hide");
        $("#divInformacoesXML").removeClass("hide");

        $("#btnCarregarXML").addClass("hide");
        $("#btnNovoDOC").removeClass("hide");
        $("#btnVisualizarDanfe").removeClass("hide");
        $("#btnVisualizarXML").removeClass("hide");
    } else {
        $("#divSelecionarArquivo").removeClass("hide");
        $("#divInformacoesXML").addClass("hide");

        $("#btnCarregarXML").removeClass("hide");
        $("#btnNovoDOC").addClass("hide");
        $("#btnVisualizarDanfe").addClass("hide");
        $("#btnVisualizarXML").addClass("hide");
    }
})


$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");

});

function NovoDOC()
{
    window.location = url + 'EnvioXML';
}

function LoadData()
{
    if ($("#btnProcurar").val() != "") {
        document.forms["frmXML"].submit();
    }
    else {
        $.SmartMessageBox({
            title: "Erros",
            content: "Selecione um arquivo!",
            buttons: '[Fechar]'
        });
    }
}

function LoadData2() {
    if ($("#btnProcurar").val() != "") {
        pFile = $('#btnProcurar').val();

        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "EnvioXml/CarregaGrid",
            dataType: 'html',
            data: {
                pFile: pFile
            },
            error: function (erro) {                
                $.smallBox({
                    title: "Erros",
                    content: "Não foi possível concluir sua solicitação.",
                    color: "#C46A69",
                    icon: "fa fa-exclamation-circle",
                    buttons: '[Fechar]',
                    timeout: 4000
                });
            },
            success: function (data) {
                $('#definicaoTabela').removeClass("hide");
                $('#definicaoTabela').html(data);
            }
        });

    }
    else {
        $.SmartMessageBox({
            title: "Erros",
            content: "Selecione um arquivo!",
            buttons: '[Fechar]'
        });

    }
}

function GetXml() {
    
    if ($("#hdfIdNF").val() != "") {
        IdNfe = $("#hdfIdNF").val();
        var urlXml = url + "EnvioXml/GetXml/" + IdNfe;
        window.open(urlXml, '_blank');
    }
    else {
        $.SmartMessageBox({
            title: "Erros",
            content: "Selecione um arquivo!",
            buttons: '[Fechar]'
        });
    }
    
}

function GetDanfe() {
    if ($("#hdfIdNF").val() != "") {
        ShowLoading("Aguarde", "Gerando o PDF do documento.");
        var IdNfe = $("#hdfIdNF").val();
        
        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Compras/GetDanfe",
            dataType: 'json',
            data: {
                pIdNfe: IdNfe
            },
            error: function () {
                HideLoading();
                $.smallBox({
                    title: "VNF NELES",
                    content: "Ocorreu um erro ao gerar o PDF do documento",
                    color: "#C46A69",
                    iconSmall: "fa fa-times fa-2x fadeInRight animated",
                    timeout: 4000
                });
            },
            success: function (oReturn) {
                HideLoading();
                window.open(oReturn.LinkCriarPdf, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
            }
        });
    }
    else {
        $.SmartMessageBox({
            title: "Erros",
            content: "Selecione um arquivo!",
            buttons: '[Fechar]'
        });
    }
    
}
