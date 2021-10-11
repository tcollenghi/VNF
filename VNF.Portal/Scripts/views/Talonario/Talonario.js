var Index = 0;
var PageSize = 30;
var IdTalonario = 0;
var StatusIntegracaoSAP = "";


function selectRow(id, StatusIntegracao)
{
    $("#row_" + id).siblings().removeClass("selecionado");
    $("#row_" + id).toggleClass("selecionado");
    $("#hdfId").val(id);
    IdTalonario = id;
    StatusIntegracaoSAP = StatusIntegracao;
}


function Modificar() {
    if (StatusIntegracaoSAP != "CONCLUÍDO") {
        if ($("#hdfId").val() != "")
            window.location = url + 'Talonario/Edit/' + IdTalonario + '?Modificar=true';
    }
    else {
        $.smallBox({
            title: "VNF NELES",
            content: "Integração SAP já realizada, não é permida alteração.<br /><br />",
            color: "#739E73",
            icon: "fa fa-file-text-o",
            timeout: 2000
        });

    }

}


function Novo() {
    
        window.location = url + 'Talonario/Edit/';
}


function setGrid()
{
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
    $('#dttTalonario').dataTable({
        "order": [[0, "desc"]],
        "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-12 hidden-xs'l>r>" +
                         "t" +
                         "<'dt-toolbar-footer'<'col-sm-6 col-xs-12 hidden-xs'i><'col-xs-12 col-sm-6'p>>",
        "autoWidth": true,
        "preDrawCallback": function () {
            // Initialize the responsive datatables helper once.
            if (!responsiveHelper_dt_basic) {
                responsiveHelper_dt_basic = new ResponsiveDatatablesHelper($('#dttTalonario'), breakpointDefinition);
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

//////////function readCookie(name) {
//////////    var nameEQ = name + "=";
//////////    var ca = document.cookie.split(';');
//////////    for (var i = 0; i < ca.length; i++) {
//////////        var c = ca[i];
//////////        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
//////////        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
//////////    }
//////////    return null;
//////////}

//////////$(document).ready(function () {
//////////    pageSetUp();
//////////    setMaskCnpj("txtCNPJ");
    
//////////    //Verifica se tem filtros na session
//////////    //var filtros = readCookie('LoadDataTal');
//////////    if (filtros != null) {
//////////        ShowLoading("Aguarde", "Procurando documentos...");
        
//////////        var aux = filtros.split('|');
//////////        $("#txtNumeroDocumento").val(aux[0]);
//////////        $("#txtCNPJ").val(aux[1]);
//////////        $("#txtPedidoCompra").val(aux[2]);
//////////        $("#txtFornecedor").val(aux[3]);
//////////        $("#TipoDocumento").val(aux[4]);
//////////        $("#TipoFrete").val(aux[5]);
//////////        $("#UnidadeMetso").val(aux[6]);
//////////        $("#Situacao").val(aux[7]);
//////////        $("#MaterialRecebido").val(aux[8]);
//////////        $("#StatusIntegracao").val(aux[9]);
//////////        $("#TipoData").val(aux[10]);
//////////        $("#txtDataDe").val(aux[11]);
//////////        $("#txtDataAte").val(aux[12]);

//////////        $("#dttTalonario tbody").empty();
//////////        $("#dttTalonario tfoot").show();
//////////        $("#hdfId").val("");
//////////        Index = 0;
//////////        $.ajax({
//////////            async: true,
//////////            cache: false,
//////////            type: 'GET',
//////////            url: url + "Talonario/LoadGrid",
//////////            dataType: 'html',
//////////            data: {
//////////                pNumeroDocumento: aux[0],
//////////                pCNPJ: aux[1],
//////////                pPedidoCompra: aux[2],
//////////                pFornecedor: aux[3],
//////////                pTipoDocumento: aux[4],
//////////                pTipoFrete: aux[5],
//////////                pUnidadeMetso: aux[6],
//////////                pSituacao: aux[7],
//////////                pMaterialRecebido: aux[8],
//////////                pStatusIntegracao: aux[9],
//////////                pTipoData: aux[10],
//////////                pDataDe: aux[11],
//////////                pDataAte: aux[12]
//////////            },
//////////            error: function (erro) {
//////////                $.SmartMessageBox({
//////////                    title: "Ocorreu um erro inesperado no sistema.",
//////////                    content: erro,
//////////                    buttons: '[Ok]'
//////////                }, function (ButtonPress, Value) {
//////////                    HideLoading();
//////////                });
//////////            },
//////////            success: function (oReturn) {
//////////                HideLoading();
//////////                $("#divGrid").html(oReturn);
//////////                setGrid(); 
//////////            }
//////////        });
//////////    }


//////////}) 

$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});

$(window).scroll(function () {
    if ($(window).scrollTop() + $(window).height() == $(document).height()) {
    }
});

$(document).keydown(function (e) {
    if (e.keyCode == 67 && e.ctrlKey) {
        if ($("#hdfId").val() != '' && $("#hdfId").val() !== undefined) {
            window.clipboardData.setData('Text', $("#hdfId").val());
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
    $("#dttTalonario tbody").empty();
    $("#dttTalonario tfoot").show();
    $("#hdfId").val("");
    Index = 0;
    LoadData();
}

function LoadData() {
    ShowLoading("Aguarde", "Procurando documentos...");
    var txtNumeroDocumento = $("#txtNumeroDocumento").val();
    var txtSerie = $("#txtSerie").val();
    var txtCNPJEmit = $("#txtCNPJEmit").val();
    var UnidadeMetso = $("#UnidadeMetso").val();
    var Finalizado = $("#Finalizado").val();
    var StatusIntegracao = $("#StatusIntegracao").val();
    var txtDataDe = $("#txtDataDe").val();
    var txtDataAte = $("#txtDataAte").val();
        
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Talonario/LoadGrid",
        dataType: 'html',
        data: {
            pStrNumeroDocumento: txtNumeroDocumento,
            pStrSerie: txtSerie,
            pStrCNPJEmit: txtCNPJEmit,
            pStrCNPJMetso: UnidadeMetso,
            pStrFinalizado: Finalizado,
            pStrStatusIntegracao: StatusIntegracao,
            pStrDataDe: txtDataDe,
            pStrDataAte: txtDataAte
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

function KeyEnter(e) { 
    if (e.keyCode == 13) {
        FilterData();
    }
}

