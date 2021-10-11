var Index = 0;
var PageSize = 30;

function selectRow(id) {
    $("#row_" + id).siblings().removeClass("selecionado");
    $("#row_" + id).toggleClass("selecionado");
    $("#hdfId").val(id);
}

function Edit(controller) {
    if ($("#hdfId").val() != "")
        window.location = url + controller + '/Edit/' + $("#hdfId").val();
}

function Modificar() {
    if ($("#hdfId").val() != "")
        window.location = url + 'Associacao/Edit/' + $("#hdfId").val();
}

function setGrid() {
    /* BASIC ;*/
    var responsiveHelper_dt_basic = undefined;
    var responsiveHelper_datatable_fixed_column = undefined;
    var responsiveHelper_datatable_col_reorder = undefined;
    var responsiveHelper_datatable_tabletools = undefined;

    var breakpointDefinition = {
        tablet: 1024,
        phone: 480
    };

    //Extensão para corrigir problema de ordenação em colunas de data
    jQuery.extend(jQuery.fn.dataTableExt.oSort, {
        "date-uk-pre": function (a) {
            var ukDatea = a.split('/');
            return (ukDatea[2] + ukDatea[1] + ukDatea[0]) * 1;
        },

        "date-uk-asc": function (a, b) {
            return ((a < b) ? -1 : ((a > b) ? 1 : 0));
        },

        "date-uk-desc": function (a, b) {
            return ((a < b) ? 1 : ((a > b) ? -1 : 0));
        }
    });



    /* COLUMN SHOW - HIDE */
    $('#dttNotaFiscal').dataTable({
        "aoColumns": [
            { "sType": "date-uk" },
            null,
            null,
            null,
            null,
            null,
            { "sType": "date-uk" },
            null
        ],

        "order": [[0, "desc"]],
        "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-12 hidden-xs'l>r>" +
            "t" +
            "<'dt-toolbar-footer'<'col-sm-6 col-xs-12 hidden-xs'i><'col-xs-12 col-sm-6'p>>",
        "autoWidth": true,
        "preDrawCallback": function () {
            // Initialize the responsive datatables helper once.
            if (!responsiveHelper_dt_basic) {
                responsiveHelper_dt_basic = new ResponsiveDatatablesHelper($('#dttNotaFiscal'), breakpointDefinition);
            }
        },
        "rowCallback": function (nRow) {
            responsiveHelper_dt_basic.createExpandIcon(nRow);
        },
        "drawCallback": function (oSettings) {
            responsiveHelper_dt_basic.respond();
        }
    });

    ///* END COLUMN SHOW - HIDE */




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
}

function readCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}

$(document).ready(function () {
    pageSetUp();
    setMaskCnpj("txtCNPJ");
    $("#TipoData").val("E");

    //Verifica se tem filtros na session
    var filtros = readCookie('LoadDataNOTF');
    if (filtros != null) {
        ShowLoading("Aguarde", "Procurando documentos...");

        var aux = filtros.split('|');
        $("#txtNFeid").val(aux[0]);//Marcio 28/08/2018 - CRXXXXX
        $("#txtNumeroDocumento").val(aux[1]);
        $("#TipoNotaFiscal").val(aux[2]);//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
        $("#txtCNPJ").val(aux[3]);
        $("#txtPedidoCompra").val(aux[4]);
        $("#txtFornecedor").val(aux[5]);
        $("#TipoDocumento").val(aux[6]);
        $("#TipoFrete").val(aux[7]);
        $("#UnidadeMetso").val(aux[8]);
        $("#Situacao").val(aux[9]);
        $("#MaterialRecebido").val(aux[10]);
        $("#StatusIntegracao").val(aux[11]);
        $("#TipoData").val(aux[12]);
        $("#txtDataDe").val(aux[13]);
        $("#txtDataAte").val(aux[14]);

        $("#dttNotaFiscal tbody").empty();
        $("#dttNotaFiscal tfoot").show();
        $("#hdfId").val("");
        Index = 0;
        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Compras/LoadDataNOTF",
            dataType: 'html',
            data: {
                pNFeid: aux[0],//Marcio 28/08/2018 - CRXXXXX
                pNumeroDocumento: aux[1],
                pTipoNotaFiscal: aux[2],//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
                pCNPJ: aux[3],
                pPedidoCompra: aux[4],
                pFornecedor: aux[5],
                pTipoDocumento: aux[6],
                pTipoFrete: aux[7],
                pUnidadeMetso: aux[8],
                pSituacao: aux[9],
                pMaterialRecebido: aux[10],
                pStatusIntegracao: aux[11],
                pTipoData: aux[12],
                pDataDe: aux[13],
                pDataAte: aux[14]
            },
            error: function (erro) {
                $.SmartMessageBox({
                    title: "Ocorreu um erro inesperado no sistema.",
                    content: erro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            },
            success: function (oReturn) {
                HideLoading();
                $("#divGrid").html(oReturn);
                setGrid();
            }
        });
    }


})

$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});


$(window).scroll(function () {
    if ($(window).scrollTop() + $(window).height() == $(document).height()) {
    }
});

$(document).keydown(function (e) {
    //alert($("#hdfId").val());
    if (e.keyCode == 67 && e.ctrlKey) {
        if ($("#hdfId").val() != '' && $("#hdfId").val() !== undefined) {
            window.clipboardData.setData('Text', $("#hdfId").val());
            //window.clipboardData.items.add($("#hdfId").val(), 'text/plain');
            $.smallBox({
                title: "VNF NELES",
                content: "Chave do DOC-e copiado com sucesso.<br/><br/>",
                color: "#739E73",
                icon: "fa fa-file-text-o",
                timeout: 4000
            });
        }
    }
});

function FilterData() {
    $("#dttNotaFiscal tbody").empty();
    $("#dttNotaFiscal tfoot").show();
    $("#hdfId").val("");
    Index = 0;
    LoadData();
}

function LoadData() {
    ShowLoading("Aguarde", "Procurando documentos...");
    var txtNumeroDocumento = $("#txtNumeroDocumento").val();
    var txtCNPJ = $("#txtCNPJ").val();
    var txtPedidoCompra = $("#txtPedidoCompra").val();
    var txtFornecedor = $("#txtFornecedor").val();
    var TipoDocumento = $("#TipoDocumento").val();
    var TipoFrete = $("#TipoFrete").val();
    var UnidadeMetso = $("#UnidadeMetso").val();
    var Situacao = $("#Situacao").val();
    var MaterialRecebido = $("#MaterialRecebido").val();
    var StatusIntegracao = $("#StatusIntegracao").val();
    var tipoData = $("#TipoData").val();
    var txtDataDe = $("#txtDataDe").val();
    var txtDataAte = $("#txtDataAte").val();
    var txtNfeid = $("#txtNFeid").val();//Marcio 28/08/2018 - CRXXXXX
    var TipoNotaFiscal = $("#TipoNotaFiscal").val(); //Marcio Spinosa - 24/04/2019 - CR00009165


    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/LoadDataNOTF",
        dataType: 'html',
        data: {
            pNfeid: txtNfeid,//Marcio 28/08/2018 - CRXXXXX
            pNumeroDocumento: txtNumeroDocumento,
            pTipoNotaFiscal: TipoNotaFiscal,//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
            pCNPJ: txtCNPJ,
            pPedidoCompra: txtPedidoCompra,
            pFornecedor: txtFornecedor,
            pTipoDocumento: TipoDocumento,
            pTipoFrete: TipoFrete,
            pUnidadeMetso: UnidadeMetso,
            pSituacao: Situacao,
            pMaterialRecebido: MaterialRecebido,
            pStatusIntegracao: StatusIntegracao,
            pTipoData: tipoData,
            pDataDe: txtDataDe,
            pDataAte: txtDataAte
        },
        error: function (erro) {
            $.SmartMessageBox({
                title: "Ocorreu um erro inesperado no sistema.",
                content: erro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            HideLoading();
            $("#divGrid").html(oReturn);
            setGrid();
            $("#demo-setting").trigger("click");
        }
    });
}

function ExportarNfe() {
    ShowLoading("Aguarde", "Exportando documentos...");
    $('html, body').animate({ scrollTop: 0 }, 'slow');

    var txtNumeroDocumento = $("#txtNumeroDocumento").val();
    var txtCNPJ = $("#txtCNPJ").val();
    var txtPedidoCompra = $("#txtPedidoCompra").val();
    var txtFornecedor = $("#txtFornecedor").val();
    var TipoDocumento = $("#TipoDocumento").val();
    var TipoFrete = $("#TipoFrete").val();
    var UnidadeMetso = $("#UnidadeMetso").val();
    var Situacao = $("#Situacao").val();
    var MaterialRecebido = $("#MaterialRecebido").val();
    var StatusIntegracao = $("#StatusIntegracao").val();
    var tipoData = $("#TipoData").val();
    var txtDataDe = $("#txtDataDe").val();
    var txtDataAte = $("#txtDataAte").val();
    var txtNfeid = $("#txtNFeid").val();//Marcio 28/08/2018 - CRXXXXX
    var TipoNotaFiscal = $("#TipoNotaFiscal").val(); //Marcio Spinosa - 24/04/2019 - CR00009165


    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/ExportarNOTF",
        dataType: 'html',
        data: {
            pNfeid: txtNfeid,//Marcio 28/08/2018 - CRXXXXX
            pNumeroDocumento: txtNumeroDocumento,
            pTipoNotaFiscal: TipoNotaFiscal, //Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
            pCNPJ: txtCNPJ,
            pPedidoCompra: txtPedidoCompra,
            pFornecedor: txtFornecedor,
            pTipoDocumento: TipoDocumento,
            pTipoFrete: TipoFrete,
            pUnidadeMetso: UnidadeMetso,
            pSituacao: Situacao,
            pMaterialRecebido: MaterialRecebido,
            pStatusIntegracao: StatusIntegracao,
            pTipoData: tipoData,
            pDataDe: txtDataDe,
            pDataAte: txtDataAte
        },
        error: function (erro) {
            $.SmartMessageBox({
                title: "Ocorreu um erro inesperado no sistema.",
                content: erro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            HideLoading();
            $("#divPostingPage").addClass("hide");
            window.location = url + 'files/export/' + oReturn;
        }
    });
}

function KeyEnter(e) {
    if (e.keyCode == 13) {
        FilterData();
    }
}