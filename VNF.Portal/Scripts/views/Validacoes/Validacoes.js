/// <reference path="../../libs/jquery-2.1.1.min.js" />

//Global var
var IdExcecao;

function selectRow(id) {
    $("#Hdnid_validacao").val(id);
}

function Salvar() {
    valcodigo = $("#txtvalcodigo").val();
    valtitulousuario = $("#txtvaltitulousuario").val();
    valtextoreprovacao = $("#txtvaltextoreprovacao").val();
    id_validacao = $("#Hdnid_validacao").val();

    var erros = "";

    if (valcodigo == "") {
        erros += "<p>Informar a descrição do Código SAP</p>"
    }
    if (valtitulousuario == "") {
        erros += "<p>Informar o valor do Titulo Usuário</p>"
    }
    if (valtextoreprovacao == "") {
        erros += "<p>Informar o valor do Texto Reprovação</p>"
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

    UrlParametro = url + "Validacoes/Salvar";
    pData = {
        pValcodigo: valcodigo,
        pValtitulousuario: valtitulousuario,
        pValtextoreprovacao: valtextoreprovacao
    }

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: UrlParametro,
        dataType: 'json',
        data: pData,
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao efetuar a Validação",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $("#txtvalcodigo").val('');
            $("#txtvaltitulousuario").val('');
            $("#txtvaltextoreprovacao").val('');
            $("#Hdnid_validacao").val('0');

            $.smallBox({
                title: "Alerta",
                content: "Validação realizada com sucesso",
                timeout: 4000,
                color: "#739E73",
                icon: "fa fa-check-circle",
            });
        }
    });

}

var idCkeckForm;
$(':checkbox').click(function () {
    var id = $(this).attr('id');
    idCkeckForm = id;
});

function Check(idRow, valCheck, Column) {


    $.SmartMessageBox({
        title: "Alerta de Editar",
        content: "Tem certeza que deseja editar este(s) item(s)",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: true,
                cache: false,
                type: 'GET',
                url: url + "Validacoes/Editar",
                dataType: 'json',
                data: {
                    pId: idRow,
                    pCheck: valCheck,
                    pColumn: Column
                },
                error: function (erro) {
                    $.smallBox({
                        title: "Erros",
                        content: "Erro ao efetuar a Validação",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                },
                success: function (oReturn) {
                    $.smallBox({
                        title: "Sucesso",
                        content: "<i class='fa fa-clock-o'></i> <i>Item(s) editado(s) com sucesso</i>",
                        color: "#659265",
                        iconSmall: "fa fa-check fa-2x fadeInRight animated",
                        timeout: 4000
                    });
                }
            });
        }
        if (ButtonPressed === "Não") {
            setTimeout(function () {
                if ("True" == valCheck.toString()) {
                    $("#" + idCkeckForm.toString()).prop("checked", "true");
                }
                else {
                    $("#" + idCkeckForm.toString()).removeAttr("checked");
                }
            }, 1000);

            $.smallBox({
                title: "Cancelado",
                content: "<i class='fa fa-clock-o'></i> <i>Item(s) não editado(s)</i>",
                color: "#C46A69",
                iconSmall: "fa fa-times fa-2x fadeInRight animated",
                timeout: 4000
            });
        }
    });
    e.preventDefault();
}

function Novo() {
    $("#idTitulo").text("Novo");

    $("#txtvalcodigo").val('');
    $("#txtvaltitulousuario").val('');
    $("#txtvaltextoreprovacao").val('');
    $("#Hdnid_validacao").val('0');
    $("#IdEdit").val('');

}

$("#deletelinha").click(function (e) {
    $.SmartMessageBox({
        title: "Alerta de Exclusão",
        content: "Tem certeza que deseja excluir este(s) item(s)",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            id_validacao = $("#Hdnid_validacao").val();
            $.ajax({
                async: true,
                cache: false,
                type: 'GET',
                url: url + "Validacoes/Excluir",
                dataType: 'json',
                data: { pid: id_validacao },
                error: function (erro) {
                    $.smallBox({
                        title: "Erros",
                        content: "Erro ao efetuar a Validação",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                },
                success: function (oReturn) {

                    $.smallBox({
                        title: "Alerta",
                        content: "Validação realizada com sucesso",
                        timeout: 4000,
                        color: "#739E73",
                        icon: "fa fa-check-circle",
                    });
                }
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

function SelectedID(pid) {
    IdExcecao = pid;
    $("#Excecao option:selected").removeAttr("selected");
    $("#dt_excecao tbody").empty();
    //$.ajax({
    //    async: true,
    //    cache: false,
    //    type: 'Get',
    //    url: url + "Validacoes/GetValidacoesExecoes",
    //    dataType: 'json',
    //    data: {
    //        id: pid
    //    },
    //    error: function (erro) {
    //        alert('erro');
    //    },
    //    success: function (oReturn) {
    //        $("#dt_excecao tbody").empty();
    //        $("#dt_excecao tbody").append(oReturn.data);

    //    }
    //});
}

function getExcecoes() {

    $.ajax({
        async: true,
        cache: false,
        type: 'Get',
        url: url + "Validacoes/GetValidacoesExecoes",
        dataType: 'json',
        data: {
            id: IdExcecao,
            pStrExecType: $("#Excecao").val()
        },
        error: function (erro) {
            alert('erro');
        },
        success: function (oReturn) {
            $("#dt_excecao tbody").empty();
            $("#dt_excecao tbody").append(oReturn.data);

        }
    });


    //$.ajax({
    //    async: true,
    //    cache: false,
    //    type: 'Get',
    //    url: url + "Validacoes/GetValidacoesExecoes",
    //    dataType: 'json',
    //    data: {
    //        id: pid
    //    },
    //    error: function (erro) {
    //        alert('erro');
    //    },
    //    success: function (oReturn) {
    //        $("#dt_excecao tbody").empty();
    //        $("#dt_excecao tbody").append(oReturn.data);

    //    }
    //});
}


$("#btAddExcecoes").click(function (e) {

    
    $("#excecoes").modal("hide");
    if (!$("#Excecao").val() == "") {
        $.SmartMessageBox({
            title: "Cadastro Excecões",
            content: $("#Excecao").find('option:selected').text(),
            buttons: '[Cancelar][Salvar]',
            input: "text",
            placeholder: "",
            inputValue: "",
            required: true
        }, function (ButtonPressed, Value) {
            if (ButtonPressed === "Salvar") {
                $("#excecoes").modal("show");
                id_validacao = $("#Hdnid_validacao").val();
                valExecpt = $("#txt1").val();
                $.ajax({
                    async: true,
                    cache: false,
                    type: 'GET',
                    url: url + "Validacoes/SalvarExcecoes",
                    dataType: 'json',
                    data: {
                        pid: id_validacao,
                        pStrValExcept: valExecpt,
                        pStrExecType: $("#Excecao").val()
                    },
                    error: function (erro) {
                        $.smallBox({
                            title: "Erros",
                            content: "Erro ao efetuar o cadastro",
                            color: "#C46A69",
                            icon: "fa fa-exclamation-circle",
                            buttons: '[Fechar]',
                            timeout: 4000
                        });
                    },
                    success: function (oReturn) {
                        getExcecoes(id_validacao);
                        $.smallBox({
                            title: "Alerta",
                            content: "cadastro realizada com sucesso",
                            timeout: 4000,
                            color: "#739E73",
                            icon: "fa fa-check-circle",
                        });
                    }
                });
            }
            if (ButtonPressed === "Cancelar") {
                $("#excecoes").modal("show");
                $.smallBox({
                    title: "Cancelado",
                    content: "<i class='fa fa-clock-o'></i> <i>Item(s) não excluido(s)</i>",
                    color: "#C46A69",
                    iconSmall: "fa fa-times fa-2x fadeInRight animated",
                    timeout: 4000
                });
            }

        });
    }
    else {
        $.SmartMessageBox({
            title: "Aviso!",
            content: "Escolher tipo de exceção a ser inclusa.",
            buttons: '[Ok]'
        }, function () {
            $("#excecoes").modal("show");
         });
    }
    
    e.preventDefault();
})

function ExcluirExcecoes(pidEx, pidVal) {
    $.SmartMessageBox({
        title: "Alerta de Exclusão",
        content: "Tem certeza que deseja excluir este(s) item(s)",
        buttons: '[Ok][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: true,
                cache: false,
                type: 'GET',
                url: url + "Validacoes/ExcluirExcecoes",
                dataType: 'json',
                data: { pid: pidEx },
                error: function (erro) {
                    $.smallBox({
                        title: "Erros",
                        content: "Erro ao efetuar a exclusão",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                },
                success: function (oReturn) {

                    $.smallBox({
                        title: "Alerta",
                        content: "Exclusão realizada com sucesso",
                        timeout: 4000,
                        color: "#739E73",
                        icon: "fa fa-check-circle",
                    });
                    
                    getExcecoes(pidVal);
                }
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
}