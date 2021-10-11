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
        url: url + "J1B1N_Imposto_CFOP/GetData",
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
                timeout: 4000
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
    $("#txtImposto").val('');
    $("#txtCFOP").val('');
    $("#txtTipoImposto").val('');
    $("#txtBase").val('');
    
    $("#hdfCFOP").val('');
    $("#hdfTipo").val('');
    $("#hdfBase").val('');
    $("#txtPlanta").attr('disabled', false);
}

function Atualizar() {
    window.location = url + 'J1B1N_Imposto_CFOP/Index';
}

function SalvarImposto() {
    txtImposto = $("#txtImposto").val();
    txtCFOP = $("#txtCFOP").val();
    txtTipoImposto = $("#txtTipoImposto").val();
    txtBase = $("#txtBase").val();
    
    txtCFOPAntigo = $("#hdfCFOP").val();
    txtTipoImpostoAntigo = $("#hdfTipo").val();
    txtBaseAntigo = $("#hdfBase").val();

    var erros = "";

    if (txtCFOP == "" || txtCFOP == 0) {
        erros += "<p>Informar o CFOP</p>"
    }

    if (txtTipoImposto == "" || txtTipoImposto == 0) {
        erros += "<p>Informar o tipo de imposto</p>"

    }

    if (erros != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 4000
        });
        return;
    }

    var UrlParametro;
    var pData;

    var Editar = $("#idItem").val();
    if (Editar == 'EDITAR') {
        UrlParametro = url + "J1B1N_Imposto_CFOP/Editar";
        pData = {
            pImposto : txtImposto, 
            pCFop : txtCFOP, 
            pTipoImposto : txtTipoImposto,
            pBase: txtBase
        }
    }
    else {
        UrlParametro = url + "J1B1N_Imposto_CFOP/Salvar";
        pData = {
            pImposto: txtImposto,
            pCFop: txtCFOP,
            pTipoImposto: txtTipoImposto,
            pBase: txtBase
        }
    }

  //  Atualizar();//Marcio Spinosa - 07/05/2018 - CR00008351

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
                content: "Erro ao efetuar ao incluir/Atualizar",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 1000
            });
        },
        success: function (oReturn) {
            $("#txtImposto").val('');
            $("#txtCFOP").val('');
            $("#txtTipoImposto").val('');
            $("#txtBase").val('');
            $("#btnSalvarParametro").removeClass("disabled");
            $("#btnFecharModal").trigger('click');

            $.smallBox({
                title: "Alerta",
                content: "Processo realizada com sucesso",
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

    pImposto = $("#hdfImposto").val();
    pCFOP = $("#hdfCFOP").val();
    pTipoImposto = $("#hdfTipo").val();
    pBase = $("#hdfBase").val();


    $("#txtIdItem").attr('disabled', true);

    $("#txtImposto").val(pImposto);
    $("#txtCFOP").val(pCFOP);
    $("#txtTipoImposto").val(pTipoImposto);
    $("#txtBase").val(pBase);

    
    $("#idItem").val(pEditar);
}

function Excluir() {
    pImposto = $("#hdfImposto").val();
    pCFop = $("#hdfCFOP").val();
    pTipoImposto = $("#hdfTipo").val();

    $.SmartMessageBox({
        title: "Excluir Imposto",
        content: "Deseja realmente excluir o Imposto?",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: false,
                cache: false,
                type: 'GET',
                url: url + "J1B1N_Imposto_CFOP/Excluir",
                dataType: 'json',
                data: {
                    pImposto: pImposto,
                    pCFop: pCFop,
                    pTipoImposto: pTipoImposto
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