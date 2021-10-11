/// <reference path="../../libs/jquery-2.1.1.min.js" />

$(window).scroll(function () {
    if ($(window).scrollTop() + $(window).height() == $(document).height()) {
    }
});

function LoadData() {
    $.ajax({
        async: false,
        cache: false,
        type: 'GET',
        url: url + "J1B1N_Direito_Fiscal/GetData",
        dataType: 'json',
        data: {
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível concluir sua solicitação.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 1000
            });
        },
        success: function (oReturn) {

            if (oReturn.data.length == 0) {

                $("#dttTabela tfoot").hide();
                ;
            } else {
                $("#dttTabela tbody").append(oReturn.data);
            }
        }
    });
}

function Novo() {
    $("#idItem").text("Novo");
    $("#txtTipoImposto").val('');
    $("#txtCFOP").val('');
    $("#txtUFDestino").val('');
    $("#txtDireitoFiscal").val('');
    $("#txtValorPadrao").val('');
    $("#hdfCFOP").val('');
    $("#hdfUF").val('');
    $("#hdfDireito").val('');
    $("#hdfValor").val('');
    $("#hdfDireitoAntigo").val('');
    $("#txtTipoImposto").attr('disabled', false);
}

function Atualizar() {
    window.location = url + 'J1B1N_Direito_Fiscal/Index';
}

function SalvarDireito() {
    txtTipoImposto = $("#txtTipoImposto").val();
    txtTipoImpostoAntigo = $("#hdfTipo").val();
    txtCFOP = $("#txtCFOP").val();
    txtUFDestino = $("#txtUFDestino").val();
    txtDireitoFiscal = $("#txtDireitoFiscal").val();
    txtDireitoFiscalAntigo = $("#hdfDireito").val();
    txtValorPadrao = $("#txtValorPadrao").val();
    txtCFOPAntigo = $("#hdfCFOP").val();
    txtDireitoAntigo = $("#hdfDireito").val();/*' Marcio Spinosa - 21/02/2019 - CR00009165*/
    txtValorAntigo = $("#hdfValor").val();
    txtufantigo = $("#hdfUF").val();

    var erros = "";

    if (txtTipoImposto == "" || txtTipoImposto == 0) {
        erros += "<p>Informar o Tipo do Imposto</p>"
    }

    if (txtUFDestino == "" || txtUFDestino == 0) {
        erros += "<p>Informar o UF de Destino</p>"

    }

    if (txtDireitoFiscal == "" || txtDireitoFiscal == 0) {
        erros += "<p>Informar o Direito Fiscal</p>"

    }

    if (erros != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 1000
        });
        return;
    }

    var UrlParametro;
    var pData;

    var Editar = $("#idItem").val();
    if (Editar == 'EDITAR') {
        UrlParametro = url + "J1B1N_Direito_Fiscal/Editar";
        pData = {
            pTipoImposto: txtTipoImpostoAntigo,
            pCFOP: txtCFOP,
            pUF: txtufantigo,
            pDireito: txtDireitoFiscal, /*' Marcio Spinosa - 21/02/2019 - CR00009165*/
            pValor: txtValorPadrao,
            pDireitoAntigo : txtDireitoAntigo
        }
    }
    else {
        UrlParametro = url + "J1B1N_Direito_Fiscal/Salvar";
        pData = {
            pTipoImposto: txtTipoImposto,
            pCFOP: txtCFOP,
            pUF: txtUFDestino,
            pDireito: txtDireitoFiscal,
            pValor: txtValorPadrao
        }
    }

    //Atualizar();//Marcio Spinosa - 07/05/2018 - CR00008351

    $.ajax({
        async: false,
        cache: false,
        type: 'GET',
        url: UrlParametro,
        dataType: 'json',
        data: pData,
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao efetuar o parâmetro",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 1000
            });
        },
        success: function (oReturn) {
            $("#txtTipoImposto").val('');
            $("#txtCFOP").val('');
            $("#txtUFDestino").val('');
            $("#txtDireitoFiscal").val('');
            $("#txtValorPadrao").val('');
            $("#btnSalvarParametro").removeClass("disabled");
            $("#btnFecharModal").trigger('click');

            $.smallBox({
                title: "Alerta",
                content: "Parâmetro realizada com sucesso",
                timeout: 1000,
                color: "#739E73",
                icon: "fa fa-check-circle",
            });
            Atualizar();
        }
    });
}

function Editar(pEditar) {
    $("#idItem").text("Editar");

    pTipo = $("#hdfTipo").val();
    pCFOP = $("#hdfCFOP").val();
    PUF = $("#hdfUF").val();
    PDireito = $("#hdfDireito").val();
    pValor = $("#hdfValor").val();

    $("#txtIdItem").attr('disabled', true);

    $("#txtTipoImposto").val(pTipo);
    $("#txtCFOP").val(pCFOP);
    $("#txtUFDestino").val(PUF);
    $("#txtDireitoFiscal").val(PDireito);
    $("#txtValorPadrao").val(pValor);

    $("#idItem").val(pEditar);
}

function Excluir() {
    pTipoImposto = $("#hdfTipo").val();
    pUF = $("#hdfUF").val();
    pCFOP = $("#hdfCFOP").val();

    $.SmartMessageBox({
        title: "Exclusão",
        content: "Deseja realmente excluir o registro?",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: false,
                cache: false,
                type: 'GET',
                url: url + "J1B1N_Direito_Fiscal/Excluir",
                dataType: 'json',
                data: {
                    pTipoImposto :pTipoImposto,
                    pUF: pUF,
                    pCFOP : pCFOP
                },
                error: function (erro) {
                    $.smallBox({
                        title: "Erros",
                        content: "Não foi possível completar sua solicitação!",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 1000
                    });
                },
                success: function (oReturn) {
                    $.smallBox({
                        title: "VNF NELES",
                        content: "Exclusão efetuado com sucesso.",
                        color: "#739E73",
                        icon: "fa fa-trash-o fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 1000
                    });
                }
            });
            Atualizar();
        }
    });
    

}