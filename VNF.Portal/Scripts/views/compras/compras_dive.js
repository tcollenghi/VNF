var Index = 0;
var PageSize = 30;
var arrDvivergencia = [];

//Modo de seleção quando tem ocorrencia
function SelectRowOcorrencia(nfe, id, codlog, codFor, codCom) {
    $("#" + id + codlog).siblings().removeClass("selecionado");
    $("#" + id + codlog).toggleClass("selecionado");
    $("#hfCodCom").val(codCom);
    $("#hfCodFor").val(codFor);
    $("#hfIdNfe").val(id);
    $("#hfCodLog").val(codlog);
    $("#btnAnular").addClass("disabled");
    $("#btnCancelar").addClass("disabled");
    $("#btnVerificar").addClass("disabled");
    $("#btnVisualizar").removeClass("disabled");
    $("#btnCriarOcorrencia").removeClass("disabled");
    $("#btnReassociar").addClass("disabled");
}

function SelectRowCodLog(nfe, id, codlog) {
    $("#" + id + codlog).siblings().removeClass("selecionado");
    $("#" + id + codlog).toggleClass("selecionado");
    $("#hfIdNfe").val(id);
    $("#hfCodLog").val(codlog);
    $("#btnAnular").removeClass("disabled");
    $("#btnCancelar").removeClass("disabled");
    $("#btnVerificar").removeClass("disabled");
    $("#btnVisualizar").removeClass("disabled"); 
    $("#btnCriarOcorrencia").addClass("disabled");
    $("#btnReassociar").removeClass("disabled");
}

function JustificativaDetalhe() {
    hfIdNfe = $("#hfIdNfe").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/StatusIntegracao",
        dataType: 'text',
        data: {
            pIdNfe: hfIdNfe
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle fadeInLeft animated",
                buttons: '[Fechar]',
                timeout: 4000
            });
            $("#btnCancelar").removeClass("disabled");
        },
        success: function (oReturn) {
            if (oReturn == "CONCLUÍDO") {
                $.smallBox({
                    title: "VNF NELES",
                    content: "Não é possível alterar a informação pois a integração com o SAP está concluída.",
                    color: "#C46A69",
                    icon: "fa fa-lock fadeInLeft animated",
                    timeout: 4000
                });
            } else {
                $("#dropDownJustificativa").val("Selecione...");
                $("#textArea").val("");
                $("#divJustificativaDetalhe").modal();
            }
        }
    });
}

function Justificar()
{
    var dropDownJustificativa = $("#dropDownJustificativa").val();
    var textArea = $("#textArea").val();
    if (dropDownJustificativa == "Selecione..." || textArea == "") {
        $.SmartMessageBox({
            title: "Erros",
            content: "Informe a justificativa e motivo!",
            buttons: '[Fechar]'
        });
        return;
    }
    Anular();
}


function AddDvivergencia() {
    if ($("#ddlTipoDivergencia option:selected").text() == "") {
    } else {
        arrDvivergencia[arrDvivergencia.length] = $("#ddlTipoDivergencia option:selected").text();
        $("#ddlTipoDivergencia option:selected").remove();
        $("#btnFilterDivergencia").removeClass("btn-default");
        $("#btnFilterDivergencia").addClass("btn-primary");
    }
}

function FilterDivergencia() {

    //$("#divFilterDivergencia").removeClass("hide");
    //$("#divFilterDivergencia").dialog({
    //    height: 400,
    //    width: 800,
    //    modal: true,
    //    resizable: false,
    //    dialogClass: "no-close",
    //    buttons: {
    //        Fechar: {
    //            text: "Fechar",
    //            class: "btn btn-default",
    //            click: function () {
    //                $(this).dialog("close");
    //            }
    //        }
    //    }
    //});
    $("#dttFilterDivergencia tbody").empty();
    for (var i = 0; i < arrDvivergencia.length; i++) {
        $("#dttFilterDivergencia tbody").append("<tr id='trDivergencia" + arrDvivergencia[i].replace(/\s+/g, '') + "' style='width:70%'><td>" + arrDvivergencia[i] + "</td><td><button type='button' class='btn btn-default btn-xs' onclick='RemoveDivergencia(&#39;" + arrDvivergencia[i] + "&#39;)'><span class='glyphicon glyphicon-remove'></span></button></td></tr>");
    }
}

function RemoveDivergencia(id) {
    var index = 0;
    for (var i = 0; i < arrDvivergencia.length; i++) {
        if (arrDvivergencia[i] == id) {
            break;
        }
        index++;
    }
    if (index > -1) {
        $("#trDivergencia" + arrDvivergencia[index].replace(/\s+/g, '')).remove();
        $("#ddlTipoDivergencia").append("<option value='" + arrDvivergencia[index] + "'>" + arrDvivergencia[index] + "</option>");
        arrDvivergencia.splice(index, 1);
    }
    if (arrDvivergencia.length == 0) {
        $("#btnFilterDivergencia").removeClass("btn-primary");
        $("#btnFilterDivergencia").addClass("btn-default");
    }
}

function FilterData() {
    $("#dttTabela tbody").empty();
    $("#dttTabela tfoot").show();
    LoadData();

}

function Visualizar() {//ok
    hfIdNfe = $("#hfIdNfe").val();
    window.location = url + 'Associacao/Edit/' + hfIdNfe;
}

function Cancelar() {
    ShowLoading("Aguarde", "Verificando nota fiscal.");
    hfIdNfe = $("#hfIdNfe").val();
    hfCodLog = $("#hfCodLog").val();
    $("#btnCancelar").addClass("disabled");
    $("#MsgBoxBack").show();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/StatusIntegracao",
        dataType: 'text',
        data: {
            pIdNfe: hfIdNfe
        },
        error: function (erro) {
            HideLoading();
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle fadeInLeft animated",
                buttons: '[Fechar]',
                timeout: 4000
            });
            $("#btnCancelar").removeClass("disabled");
        },
        success: function (oReturn) {
            if (oReturn == "CONCLUÍDO") {
                HideLoading();
                $("#btnCancelar").removeClass("disabled");
                $.smallBox({
                    title: "VNF NELES",
                    content: "Não é possível alterar a informação pois a integração com o SAP está concluída.",
                    color: "#C46A69",
                    icon: "fa fa-lock fadeInLeft animated",
                    timeout: 4000
                });
            } else {
                $.SmartMessageBox({
                    title: "Cancelar documento",
                    content: "Deseja realmente cancelar o documento?",
                    buttons: '[Não][Sim]'
                }, function (ButtonPressed) {
                    if (ButtonPressed === "Sim") {
                        $.ajax({
                            async: true,
                            cache: false,
                            type: 'GET',
                            url: url + "Compras/CancelarDIVE",
                            dataType: 'json',
                            data: {
                                pIdNfe: hfIdNfe
                            },
                            error: function (erro) {
                                HideLoading();
                                $.smallBox({
                                    title: "Erros",
                                    content: "Não foi possível completar sua solicitação!",
                                    color: "#C46A69",
                                    icon: "fa fa-exclamation-circle fadeInLeft animated",
                                    buttons: '[Fechar]',
                                    timeout: 4000
                                });
                                $("#btnCancelar").removeClass("disabled");
                            },
                            success: function (oReturn) {
                                HideLoading();
                                $.smallBox({
                                    title: "VNF NELES",
                                    content: "Cancelamento efetuado com sucesso.",
                                    color: "#739E73",
                                    icon: "fa fa-trash-o fadeInLeft animated",
                                    buttons: '[Fechar]',
                                    timeout: 4000
                                });
                                $("#" + hfIdNfe + hfCodLog).remove();
                                $("#hfIdNfe").val("");
                                $("#hfCodLog").val("");
                                $("#btnAnular").addClass("disabled");
                                $("#btnCancelar").addClass("disabled");
                                $("#btnVerificar").addClass("disabled");
                                $("#btnVisualizar").addClass("disabled");
                                $("#btnCriarOcorrencia").addClass("disabled");
                                $("#btnReassociar").addClass("disabled");
                            }
                        });
                    }

                    if (ButtonPressed === "Não") {
                        HideLoading();
                        $("#btnCancelar").removeClass("disabled");
                    }
                });
            }
        }
    });
}

function Anular() {
    hfIdNfe = $("#hfIdNfe").val();
    hfCodLog = $("#hfCodLog").val();
    dropDownJustificativa = $("#dropDownJustificativa").val();
    textArea = $("#textArea").val();
    
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/AnularDIVE",
        dataType: 'json',
        data: {
            NfeId: hfIdNfe,
            CodLog: hfCodLog,
            Justificativa: dropDownJustificativa,
            Detalhe: textArea
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            if (oReturn.result === "ok"){
                $.smallBox({
                    title: "VNF NELES",
                    content: "Anulação efetuada com sucesso.",
                    color: "#739E73",
                    icon: "fa fa-check-circle",
                    timeout: 4000
                });
            } else {
                $.smallBox({
                    title: "VNF NELES",
                    content: oReturn.data,
                    color: "#739E73",
                    icon: "fa fa-check-circle",
                    timeout: 4000
                });
            }

            $("#" + hfIdNfe + hfCodLog).remove();
            $("#hfIdNfe").val("");
            $("#hfCodLog").val("");
            $("#btnAnular").addClass("disabled");
            $("#btnCancelar").addClass("disabled");
            $("#btnVerificar").addClass("disabled");
            $("#btnVisualizar").addClass("disabled");
            $("#btnCriarOcorrencia").addClass("disabled");
            $("#btnFecharJustificativa").trigger("click");
            $("#btnReassociar").addClass("disabled");
        }
    });
}

function Anular2() {
    hfIdNfe = $("#hfIdNfe").val();
    hfCodLog = $("#hfCodLog").val();
    dropDownJustificativa = $("#dropDownJustificativa").val();
    textArea = $("#textArea").val();

    if (typeof FormData == "undefined") {
        var formdata = [];
    } else {
        var formdata = new FormData();
    }

    formdata.append("NfeId", hfIdNfe);
    formdata.append("CodLog", hfCodLog);
    formdata.append("Justificativa", dropDownJustificativa);
    formdata.append("Detalhe", textArea);
    
    $('html, body').animate({ scrollTop: 0 }, 'slow');
    ShowLoading("Aguarde", "Anulando divergência");
    $.ajax({
        url: url + "Compras/AnularDIVE",
        type: 'POST',
        data: formdata,
        processData: false,
        contentType: false,
        dataType: "json",
        error: function (oReturn) {
            $.SmartMessageBox({
                title: "VNF NELES",
                content: "Erro ao criar divergência, tente novamente. Caso o problema persista contate o suporte.",
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function(oReturn) {
            if (oReturn.result === "ok") {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Divergência anulada com sucesso!",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });

                $("#" + hfIdNfe + hfCodLog).remove();
            } else {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: oReturn.data,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            }
            $("#hfIdNfe").val("");
            $("#hfCodLog").val("");
            $("#btnAnular").addClass("disabled");
            $("#btnCancelar").addClass("disabled");
            $("#btnVerificar").addClass("disabled");
            $("#btnVisualizar").addClass("disabled");
            $("#btnCriarOcorrencia").addClass("disabled");
            $("#btnFecharJustificativa").trigger("click");
            $("#btnReassociar").addClass("disabled");
        }
    });
}


function Verificar() {
    $("#btnVerificar").addClass("disabled");
    $("#btnReassociar").addClass("disabled");
    ShowLoading("Aguarde", "validando informações da nota fiscal e pedido de compra");
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/VerificarDIVE",
        dataType: 'json',
        data: {
            pIdNfe: hfIdNfe.value
        },
        error: function (erro) {
            HideLoading();
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
            $("#btnVerificar").removeClass("disabled");
            $("#btnReassociar").removeClass("disabled");
        },
        success: function (oReturn) {
            if (oReturn.mensagem != "") {
                $.SmartMessageBox({
                    title: "Não é possível associar este documento",
                    content: oReturn.mensagem,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            } else {
                HideLoading();
                $.smallBox({
                    title: "VNF NELES",
                    content: "Verificação efetuada com sucesso!",
                    color: "#739E73",
                    icon: "fa fa-check-circle",
                    timeout: 4000
                });
                $("#btnVerificar").removeClass("disabled");
                $("#btnReassociar").removeClass("disabled");
                window.location.reload();
            }
        }
    });
}

function ReassociarEmMassa() {

    var rows = $("#dttTabela").dataTable().fnGetNodes();
    var ChavesAcesso = "''";
    var ArrChavesAcesso = [];
    for (var i = 0; i < rows.length; i++) {
        var row = $("#dttTabela").dataTable().fnGetData(rows[i]);
 
        if ($.isNumeric(row[0])) {


            if ($.inArray(row[0], ArrChavesAcesso) === -1) {
                ArrChavesAcesso.push(row[0]);
                ChavesAcesso = ChavesAcesso + row[0] + "'',''";
            }
        }
    }

    //Remove a ultima virgula e aspas
    ChavesAcesso = ChavesAcesso.substr(0, ChavesAcesso.length - 3);
    $("#btnReassociar").addClass("disabled");
    ShowLoading("Aguarde", "gerando lote de documentos para reassociação.");
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Compras/ReassociarEmMassa",
        dataType: 'json',
        data: {
            pIdNfe: ChavesAcesso
        },
        error: function (oReturn) {
            HideLoading();
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação, caso o erro persista contate o suporte!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
            $("#btnReassociar").removeClass("disabled");
        },
        success: function (oReturn) {
            if (oReturn.result === "false") {
                $.SmartMessageBox({
                    title: "Erro ao processar reassociação de documentos. Exception: " + oReturn.mensagem,
                    content: oReturn.mensagem,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            } else {
                HideLoading();
                $.smallBox({
                    title: "VNF NELES",
                    content: "Solicitação para reassociação de documentos completada com sucesso! Os documentos serão reprocessados em um prazo de até 30 minutos.",
                    color: "#739E73",
                    icon: "fa fa-check-circle",
                    timeout: 5000
                });
                $("#btnReassociar").removeClass("disabled");
                window.location.reload();
            }
        }
    });
}

function LoadData() {//ok
    if (
        $("#Situacao").val() == "" &&
        $("#Motivo").val() == "" &&
        $("#txtCodigoComprador").val() == "" &&
        $("#txtNumeroPO").val() == "" &&
        $("#txtDocE").val() == "" &&
        $("#txtDataEmissaoAte").val() == "" &&
        $("#txtDataEmissaoDe").val() == "" &&
        $("#txtDataDivergenciaDe").val() == "" &&
        $("#txtDataDivergenciaAte").val() == ""
       ) {
        $.SmartMessageBox({
            title: "Erros",
            content: "Preencha os campos de busca!",
            buttons: '[Fechar]'
        });
        return;
    }
    Situacao = $("#Situacao").val();
    Motivo = $("#Motivo").val();
    txtCodigoComprador = $("#txtCodigoComprador").val();
    txtNumeroPO = $("#txtNumeroPO").val();
    txtDocE = $("#txtDocE").val();
    txtFornecedor = $("#txtFornecedor").val();
    txtDataEmissaoAte = $("#txtDataEmissaoAte").val();
    txtDataEmissaoDe = $("#txtDataEmissaoDe").val();
    txtDataDivergenciaDe = $("#txtDataDivergenciaDe").val();
    txtDataDivergenciaAte = $("#txtDataDivergenciaAte").val();
    var cboPrioridade = $("#cboPrioridade option:selected").val();
    var cboOrigem = $("#cboOrigem").val();
    var Concatena = '';
    for (var i = 0; i < arrDvivergencia.length; i++) {
        Concatena += arrDvivergencia[i] + ";";
    }

    ShowLoading("Aguarde", "Procurando divergências...");
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/GetDataDivergencia",
        dataType: 'html',
        data: {
            txtDocE: txtDocE,
            Motivo: Motivo,
            Situacao: Situacao,
            txtNumeroPO: txtNumeroPO,
            txtCodigoComprador: txtCodigoComprador,
            txtFornecedor: txtFornecedor,
            txtDataEmissaoDe: txtDataEmissaoDe,
            txtDataEmissaoAte: txtDataEmissaoAte,
            txtDataDivergenciaDe: txtDataDivergenciaDe,
            txtDataDivergenciaAte: txtDataDivergenciaAte,
            arrDivergencia: Concatena,
            cboPrioridade: cboPrioridade,
            cboOrigem: cboOrigem
        },
        error: function (erro) {
            HideLoading();
            $("#demo-setting").trigger("click");
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            HideLoading();
            $("#demo-setting").trigger("click");
            $('#divGrid').html(oReturn);

            $("#btnAnular").addClass("disabled");
            $("#btnCancelar").addClass("disabled");
            $("#btnVerificar").addClass("disabled");
            $("#btnVisualizar").addClass("disabled"); 
            $("#btnCriarOcorrencia").addClass("disabled");
            $("#btnReassociar").addClass("disabled");
        }
    });
}

function ExportarDIVE() {//ok
    $("#divPostingPage").removeClass("hide");
    $("#divPostingPage").show();
    $('html, body').animate({ scrollTop: 0 }, 'slow');
    Situacao = $("#Situacao").val();
    Motivo = $("#Motivo").val();
    txtCodigoComprador = $("#txtCodigoComprador").val();
    txtNumeroPO = $("#txtNumeroPO").val();
    txtDocE = $("#txtDocE").val();
    txtDataEmissaoAte = $("#txtDataEmissaoAte").val();
    txtDataEmissaoDe = $("#txtDataEmissaoDe").val();
    txtDataDivergenciaDe = $("#txtDataDivergenciaDe").val();
    txtDataDivergenciaAte = $("#txtDataDivergenciaAte").val();
    var Concatena = '';
    for (var i = 0; i < arrDvivergencia.length; i++) {
        Concatena += arrDvivergencia[i] + ";";
    }
    ShowLoading("Aguarde", "Exportando divergências...");
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/ExportarDIVE",
        dataType: 'html',
        data: {
            txtDocE: txtDocE,
            Motivo: Motivo,
            Situacao: Situacao,
            txtNumeroPO: txtNumeroPO,
            txtCodigoComprador: txtCodigoComprador,
            txtDataEmissaoDe: txtDataEmissaoDe,
            txtDataEmissaoAte: txtDataEmissaoAte,
            txtDataDivergenciaDe: txtDataDivergenciaDe,
            txtDataDivergenciaAte: txtDataDivergenciaAte,
            arrDivergencia: Concatena
        },
        error: function () {
            HideLoading();
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            HideLoading();
            window.location = url + 'files/export/' + oReturn;
        }
    });
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
	$("#Situacao").val("ATIVO"); 
    //pega os valores do cookie de pesquisa

	var filtros = readCookie('GetDataDivergencia');
	if (filtros != null) {
	    var aux = filtros.split('|');
	    $("#divGrid tbody").empty();
	    ShowLoading("Aguarde", "Procurando divergências...");
	    $.ajax({
	        async: true,
	        cache: false,
	        type: 'GET',
	        url: url + "Compras/GetDataDivergencia",
	        dataType: 'html',
	        data: {
	            txtDocE: aux[0],
	            Motivo: aux[1],
	            Situacao: aux[2],
	            txtNumeroPO: aux[3],
	            txtCodigoComprador: aux[4],
	            txtFornecedor: aux[5],
	            txtDataEmissaoDe: aux[6],
	            txtDataEmissaoAte: aux[7],
	            txtDataDivergenciaDe: aux[8],
	            txtDataDivergenciaAte: aux[9],
	            arrDivergencia: aux[10]
	        },
	        error: function (erro) {
	            HideLoading();
	            $.smallBox({
	                title: "Erros",
	                content: "Não foi possível completar sua solicitação!",
	                color: "#C46A69",
	                icon: "fa fa-exclamation-circle",
	                buttons: '[Fechar]',
	                timeout: 4000
	            });
	        },
	        success: function (oReturn) {
	            HideLoading();
	            $('#divGrid').html(oReturn);
	            $("#btnAnular").addClass("disabled");
	            $("#btnCancelar").addClass("disabled");
	            $("#btnVerificar").addClass("disabled");
	            $("#btnVisualizar").addClass("disabled"); 
	            $("#btnCriarOcorrencia").addClass("disabled");
	            $("#btnReassociar").addClass("disabled");
	        }
	    });
	}
	else {
	    if ($("#txtCodigoComprador").val() != "") {
	        FilterData();
	    }
	}
})


$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});




function KeyEnter(e) {
    if (e.keyCode == 13) {
        FilterData();
    }
}

function GetOcorrencia() {
     
    var IdOcorrencia = $("#hfIdNfe").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/GetOcorrencia",
        dataType: 'json',
        data: { IdOcorrencia: IdOcorrencia },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $("#txtMotivo").val(oReturn.MotivoCorrecao);
            $("#DataRecebimento").val(oReturn.DataRecebimento);
            $("#txtDataEsperada").val(oReturn.DataEsperada);
            if (oReturn.Status == "Finalizado") {
                $("#btnFinalizarOc").hide();
                $("#btnEncaminharOC").hide(); 
            }
            else {
                $("#btnFinalizarOc").show();
                $("#btnEncaminharOC").show();
            }
        }
    });
}

function MostrarOcorrencia() {
    $("#tabOcorrencia").removeClass("hide");

    $("#tabHistorico").addClass("hide"); 
}

function MostrarHistorico() {
    var IdOcorrencia = $("#hfIdNfe").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/GetOcorrenciaHistorico",
        dataType: 'text',
        data: { IdOcorrencia: IdOcorrencia },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $("#dtHistorico tbody").empty();
            $("#dtHistorico tbody").append(oReturn);
        }
    });
    $("#tabHistorico").removeClass("hide"); 
    $("#tabOcorrencia").addClass("hide");
} 

function FinalizaOcorrencia() {
    var Comentario = $("#txtCometario").val();
    var IdOcorrencia = $("#hfIdNfe").val();
    if (Comentario == "") {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Preencha as informações obrigatórias",
            buttons: '[Ok]'
        });
    }
    else {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Deseja finalizar a ocorrência?",
            buttons: '[No][Yes]'
        }, function (ButtonPressed) {
            if (ButtonPressed === "Yes") {
                if (typeof FormData == "undefined") {
                    var formdata = [];
                } else {
                    var formdata = new FormData();
                }


                var fileInput = document.getElementById("FileAttachment");
                if (fileInput.files != null) {
                    for (i = 0; i < fileInput.files.length; i++) {
                        formdata.append(fileInput.files[i].name, fileInput.files[i]);
                    }
                }

                formdata.append("IdOcorrencia", IdOcorrencia);
                formdata.append("Comentario", Comentario);
                var params;
                if (typeof FormData == "undefined") {
                    params = JSON.stringify(formdata);
                } else {
                    params = formdata;
                }

                $('html, body').animate({ scrollTop: 0 }, 'slow');

                var xhr = new XMLHttpRequest();
                xhr.open("POST", url + "Compras" + "/" + "FinalizaOcorrencia", true);
                xhr.setRequestHeader("Content-length", params.length);
                xhr.setRequestHeader("Connection", "close");
                xhr.send(params);

                xhr.onload = function () {
                    window.scrollTo(0, 0);
                    if (xhr.readyState == 4) {
                        if (xhr.status == 200) {

                            if (xhr.responseText != "") {
                                var postReturn = JSON.parse(xhr.responseText);
                            }

                            if (postReturn != undefined && postReturn.result == "Error") {
                                $.smallBox({
                                    title: "Erros",
                                    content: "Não foi possível completar sua solicitação!",
                                    color: "#C46A69",
                                    icon: "fa fa-exclamation-circle",
                                    buttons: '[Fechar]',
                                    timeout: 4000
                                });
                            } else {
                                 
                                $.smallBox({
                                    title: "VNF NELES",
                                    content: "Ocorrência finalizada com sucesso.",
                                    color: "#739E73",
                                    icon: "fa fa-check-circle",
                                    timeout: 4000
                                });
                                $("#txtCometario").val("");
                                $("#divOcorrencia").modal('hide'); 
                            }
                        }
                        else {
                            $.smallBox({
                                title: "Erros",
                                content: "Não foi possível completar sua solicitação!",
                                color: "#C46A69",
                                icon: "fa fa-exclamation-circle",
                                buttons: '[Fechar]',
                                timeout: 4000
                            });
                        }
                    }
                }
            } 
        });
    }
}

function CarregaDadosEncaminhamento() {
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/GetMotivoOcorrencia",
        dataType: 'text',
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível carregar os motivos de ocorrência!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $("#cboMotivoDivergenciaEncaminhar").empty();
            $("#cboMotivoDivergenciaEncaminhar").append(oReturn);
        }
    });
}

function cboMotivoChange() {
    $("#cboResponsavelEncaminhar").val("");
    $("#txtEmail").val("");
    SelecionaMotivoCorrecaoEncaminhamento();
}

function SelecionaMotivoCorrecaoEncaminhamento() {
    var idMotivo = $("#cboMotivoDivergenciaEncaminhar option:selected").val();
    var CodCom = $("#hfCodCom").val();
    var CodFor = $("#hfCodFor").val();
    var Responsavel = $("#cboResponsavelEncaminhar option:selected").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/GetResponsavelMotivoCorrecao",
        dataType: 'json', 
        data: { idMotivo: idMotivo, CodFor: CodFor, CodCom: CodCom, Responsavel: Responsavel },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível carregar os motivos de ocorrência!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            if (oReturn != null) {
                $("#cboResponsavelEncaminhar").val(oReturn.Responsavel);
                if (oReturn.Responsavel == "Manual") {
                    $("#txtEmail").val("");
                    $("#txtEmail").prop('disabled', false);
                }
                else {
                    $("#txtEmail").val(oReturn.Email);
                    $("#txtEmail").prop('disabled', true);
                }
            }
        }
    }); 
}

function EncaminhaOcorrencia() {
    var IdOcorrencia = $("#hfIdNfe").val();
    var IdMotivoCorrecao = $("#cboMotivoDivergenciaEncaminhar option:selected").val();
    var Responsavel = $("#cboResponsavelEncaminhar option:selected").val();
    var Email = $("#txtEmail").val();
    var Comentario = $("#txtCometarioEncaminhar").val();

    if (IdMotivoCorrecao == "" || Responsavel == "" || Email == "" || Comentario == "") {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Preencha todas as informações obrigatórias",
            buttons: '[Ok]'
        });
    }
    else {
        $.SmartMessageBox({
            title: "VNF NELES",
            content: "Confirma o envio da ocorrência?",
            buttons: '[No][Yes]'
        }, function (ButtonPressed) {
            if (ButtonPressed === "Yes") {
                if (typeof FormData == "undefined") {
                    var formdata = [];
                } else {
                    var formdata = new FormData();
                }


                var fileInput = document.getElementById("FileAttachmentEncaminhar");
                if (fileInput.files != null) {
                    for (i = 0; i < fileInput.files.length; i++) {
                        formdata.append(fileInput.files[i].name, fileInput.files[i]);
                    }
                }

                formdata.append("IdOcorrencia", IdOcorrencia);
                formdata.append("Comentario", Comentario);
                formdata.append("IdMotivoCorrecao", IdMotivoCorrecao);
                formdata.append("Responsavel", Responsavel);
                formdata.append("Email", Email);
                var params;
                if (typeof FormData == "undefined") {
                    params = JSON.stringify(formdata);
                } else {
                    params = formdata;
                }

                $('html, body').animate({ scrollTop: 0 }, 'slow');

                var xhr = new XMLHttpRequest();
                xhr.open("POST", url + "Compras" + "/" + "EncaminhaOcorrencia", true);
                xhr.setRequestHeader("Content-length", params.length);
                xhr.setRequestHeader("Connection", "close");
                xhr.send(params);

                xhr.onload = function () {
                    window.scrollTo(0, 0);
                    if (xhr.readyState == 4) {
                        if (xhr.status == 200) {

                            if (xhr.responseText != "") {
                                var postReturn = JSON.parse(xhr.responseText);
                            }

                            if (postReturn != undefined && postReturn.result == "Error") {
                                $.smallBox({
                                    title: "Erros",
                                    content: "Não foi possível completar sua solicitação!",
                                    color: "#C46A69",
                                    icon: "fa fa-exclamation-circle",
                                    buttons: '[Fechar]',
                                    timeout: 4000
                                });
                            } else {

                                $.smallBox({
                                    title: "VNF NELES",
                                    content: "Ocorrência encaminhada com sucesso.",
                                    color: "#739E73",
                                    icon: "fa fa-check-circle",
                                    timeout: 4000 
                                });
                                $("#cboMotivoDivergenciaEncaminhar option:selected").val("");
                                $("#cboResponsavelEncaminhar option:selected").val("");
                                $("#txtEmail").val("");
                                $("#txtCometario").val("");
                                $("#divOcorrencia").modal('hide');
                                $("#divEncaminharOcorrencia").modal('hide');
                            }
                        }
                        else {
                            $.smallBox({
                                title: "Erros",
                                content: "Não foi possível completar sua solicitação!",
                                color: "#C46A69",
                                icon: "fa fa-exclamation-circle",
                                buttons: '[Fechar]',
                                timeout: 4000
                            });
                        }
                    }
                }
            }
        });
    }


}

function CheckMotivo()
{
    if ($("#Situacao").val() == "INATIVO")
    {
        $("#Motivo").removeAttr('disabled');
    } else {
        $("#Motivo").attr('disabled', 'disabled');
    }
}