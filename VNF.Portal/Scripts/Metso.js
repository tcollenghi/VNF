
//-----------------------------------------------------------------------------------------//
//----------------------------------> STANDARD VARIABLE <----------------------------------//
//-----------------------------------------------------------------------------------------//
var postReturn = null;
var postReturnValue = null;
var pathArray = window.location.href.split('/');
var protocol = pathArray[0];
var host = pathArray[2];
var app = pathArray[3];
var url = '';
if (host.indexOf("localhost") > -1) {
    url = protocol + '//' + host + '/';
} else {
    url = protocol + '//' + host + '/' + app + '/';
}


//----------------------------------------------------------------------------------//
//----------------------------------> INDEX PAGE <----------------------------------//
//----------------------------------------------------------------------------------//

//function SelectRow(controller, id) {
//    $(".info").removeClass("info");
//    $(".disabled").removeClass("disabled");
//    $("#" + $.trim(id)).addClass("info");
//    $("#hfId" + controller).val($.trim(id));    
  
//}


function ConfirmDeletion() {
    $("#divConfirmDeletion").removeClass("hide")
}

function CancelDeletion() {
    $("#divConfirmDeletion").addClass("hide")
}

function Delete(controller) {
    $.ajax({
        async: false,
        type: "POST",
        cache: false,
        url: url + controller + "/Delete",
        data: { id: $("#hfId" + controller).val() },
        error: function () {
        },
        success: function () {
            $("." + $("#hfId" + controller).val()).remove();
            $("#divConfirmDeletion").addClass("hide");
            $("#btnEdit").addClass("disabled");
            $("#btnDelete").addClass("disabled");
        }
    });
}


//---------------------------------------------------------------------------------//
//----------------------------------> EDIT PAGE <----------------------------------//
//---------------------------------------------------------------------------------//

function ValidateGroup(groupName) {
    var ValidacaoOk = true;
    $("#div" + groupName + " *").filter(':input').each(function (key, value) {
        $(this).parent(".form-group").removeClass("has-error")
        if ($(this).attr("id") != undefined && $(this).hasClass("required") && $(this).val() == "") {
            $(this).parent(".form-group").addClass("has-error");
            ValidacaoOk = false;
        }
    });

    $("#div" + groupName + " *").filter('select').each(function (key, value) {
        $(this).parent().parent(".form-group").removeClass("has-error")
        if ($(this).attr("id") != undefined && $(this).hasClass("required") && $("#" + $(this).attr("id") + " option:selected").val() == "") {
            $(this).parent().parent(".form-group").addClass("has-error");
            ValidacaoOk = false;
        }
    });

    //Anexos individuais
    $("#div" + groupName + " * [id*=TextInput]").filter('input').each(function (key, value) {
        $(this).parent().parent().parent().parent(".form-group").removeClass("has-error")
        if ($(this).attr("id") != undefined && $(this).hasClass("required") && $(this).val() == "") {
            $(this).parent().parent().parent().parent(".form-group").addClass("has-error");
            ValidacaoOk = false;
        }
    });

    //Anexos múltiplos
    $("#div" + groupName + " * [id*=hfQuantityAttachments]").filter('input').each(function (key, value) {
        $(this).parent().parent().parent().removeClass("panel-danger")
        if ($(this).val() == "0") {
            $(this).parent().parent().parent().addClass("panel-danger");
            ValidacaoOk = false;
        }
    });

    return ValidacaoOk;
}

function FormIsValid(controller) {
    var ValidacaoOk = true;
    $("#frm" + controller + " *").filter(':input').each(function (key, value) {
        $(this).parent(".form-group").removeClass("has-error")
        if ($(this).attr("id") != undefined && $(this).hasClass("required") && $(this).val() == "") {
            $(this).parent(".form-group").addClass("has-error");
            ValidacaoOk = false;
        }

        if ($(this).attr("id") != undefined && $(this).hasClass("picture") && $(this).val() != "") {
            var fileInput = document.getElementById($(this).attr("id"));
            if (fileInput.files != null) {
                var ext = $(this).val().split('.').pop().toLowerCase();
                if ($.inArray(ext, ['gif', 'png', 'jpg', 'jpeg']) == -1) {
                    $(this).parent(".form-group").addClass("has-error");
                    ValidacaoOk = false;
                }
            }
        }
    });

    return ValidacaoOk;
}

function submitForm(controller) {

    var formName = "frm" + controller;

    $("#" + formName + " *").filter(':input').each(function (key, value) {
        if ($(this).attr("id") != undefined) {
            if ($(this).hasClass("metso-number") == true) {
                $(this).val($(this).val().replace(".", ""));
            }
            if ($(this).hasClass("metso-float") == true) {
                $(this).val($(this).val().replace(".", ""));
            }
            if ($(this).hasClass("metso-floatprecision") == true) {
                $(this).val($(this).val().replace(".", ""));
            }
            if ($(this).hasClass("metso-currencyreal") == true) {
                $(this).val($(this).val().Replace("R$", "").Trim().replace(".", ""));
            }
            if ($(this).hasClass("metso-currencydolar") == true) {
                $(this).val($(this).val().replace("US$ ", "").replace(",", "").replace(".", ","));
            }
            if ($(this).hasClass("metso-currencyeuro") == true) {
                $(this).val($(this).val().replace("€ ", "").replace(",", "").replace(".", ","));
            }
        }
    });

    $("#" + formName).submit();
}

function PostPage(action, controller, reloadPage) {
    if (FormIsValid(controller) == false) {
        return false;
    }else{
        if (typeof FormData == "undefined") {
            var formdata = [];
        } else {
            var formdata = new FormData();
        }

        $("#frm" + action + " *").filter(':input').each(function (key, value) {
            if ($(this).attr("id") != undefined) {
                
                if ($(this).attr("type") == "checkbox") {
                    formdata.append($(this).attr("id"), $(this).is(":checked"));
                } else if ($(this).is('select')) {
                    formdata.append($(this).attr("id"), $("#" + $(this).attr("id") + " option:selected").val());
                } else {
                    formdata.append($(this).attr("id"), $(this).val());
                }

                var fileInput = document.getElementById($(this).attr("id"));
                if (fileInput.files != null) {
                    for (i = 0; i < fileInput.files.length; i++) {
                        formdata.append(fileInput.files[i].name, fileInput.files[i]);
                    }
                }
            }
        });

        var params;
        if (typeof FormData == "undefined") {
            params = JSON.stringify(formdata);
        } else {
            params = formdata;
        }
        
        $("#divFalhaSalvar").addClass("hide");
        $("#divConfirmacaoSalvar").addClass("hide");

        $("#divPostingPage").removeClass("hide");
        $("#divPostingPage").show();
        $('html, body').animate({ scrollTop: 0 }, 'slow');
        
        var xhr = new XMLHttpRequest();
        xhr.open("POST", url + controller + "/" + action, true);
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
                        $("#divPostingPage").addClass("hide");
                        $("#divConfirmacaoSalvar").addClass("hide");
                        $("#txtErrorMessage").append(postReturn.data);
                        $("#txtErrorMessage").removeClass("hide");
                        $("#divFalhaSalvar").removeClass("hide");
                        if (reloadPage == true) {
                            var timer = window.setTimeout(function () {
                                location.reload();
                                window.clearTimeout(timer);
                            }, 5000);
                        }
                        else {
                            return true;
                        }

                    } else if (reloadPage == undefined || reloadPage == false) {
                        if (postReturn != undefined) {
                            postReturnValue = postReturn.data;
                        }

                        $("#divPostingPage").addClass("hide");
                        $("#txtErrorMessage").addClass("hide");
                        $("#divConfirmacaoSalvar").removeClass("hide")
                        $("#divFalhaSalvar").addClass("hide");
                        var timer = window.setTimeout(function () {
                            $("#divConfirmacaoSalvar").addClass("hide")
                            window.clearTimeout(timer);
                        }, 5000);
                        return false;

                    } else {
                        location.reload();
                    }
                }
                else {
                    $("#divPostingPage").addClass("hide");
                    $("#divConfirmacaoSalvar").addClass("hide");
                    $("#txtErrorMessage").text(xhr.responseText);
                    $("#divFalhaSalvar").removeClass("hide");
                    return false;
                }
            }
        }

        return true;
    }
}

//------------------------------------------------------------------------------------//
//----------------------------------> MASKED INPUT <----------------------------------//
//------------------------------------------------------------------------------------//

jQuery(function ($) {
    $(".metso-cep").mask("99.999-999");
    $(".metso-phone").mask("(99) 9999-9999");
    $(".metso-cellphone").mask("(99) 99999-9999");
    $(".metso-number").maskMoney({ symbol: "", thousands: ".", decimal: ",", symbolStay: false, allowZero: true, precision: 0 });
    $(".metso-float").maskMoney({ symbol: "", thousands: ".", decimal: ",", symbolStay: false, allowZero: true, precision: 2 });
    $(".metso-floatprecision").maskMoney({ symbol: "", thousands: ".", decimal: ",", symbolStay: false, allowZero: true, precision: 5 });
    $(".metso-currencyreal").maskMoney({ symbol: "R$ ", thousands: ".", decimal: ",", symbolStay: true, allowZero: true });
    $(".metso-currencydolar").maskMoney({ symbol: "US$ ", thousands: ",", decimal: ".", symbolStay: true, allowZero: true });
    $(".metso-currencyeuro").maskMoney({ symbol: "€ ", thousands: ",", decimal: ".", symbolStay: true, allowZero: true });
});


jQuery(function ($) {
    $('.metso-email').on('blur', function (e) {
        $(this).parent(".form-group").removeClass("has-error");

        var emailList = $(this).val().replace(/,/g, ";").split(";");
        var quantity = emailList.length;
        var isValid = true;

        for (var i = 0; i < quantity; i++) {
            if (!validarEmail($.trim(emailList[i])) && $.trim(emailList[i]) != "") {
                isValid = false;
                break;
            }

        }

        if (!isValid) {
            $(this).parent(".form-group").addClass("has-error");
            $(this).focus();
        }
    });
});


jQuery(function ($) {
    $(".metso-date").mask("99/99/9999", { completed: function () {
        $(this).parent(".form-group").removeClass("has-error")
        if (validarData(this.val()) == false) {
            $(this).parent(".form-group").addClass("has-error");
        }
    }
    });

    $('.metso-date').on('blur', function (e) {
        $(this).parent(".form-group").removeClass("has-error")
        if (validarData($(this).val()) == false && $(this).val().length > 0) {
            $(this).parent(".form-group").addClass("has-error");
            $(this).focus();
        }
    });
});

jQuery(function ($) {
    $(".metso-datetime").mask("99/99/9999 99:99", { completed: function () {
        $(this).parent(".form-group").removeClass("has-error")
        if (validarData(this.val()) == false) {
            $(this).parent(".form-group").addClass("has-error");
        }
    }
    });

    $('.metso-datetime').on('blur', function (e) {
        $(this).parent(".form-group").removeClass("has-error")
        if (validarData($(this).val()) == false && $(this).val().length > 0) {
            $(this).parent(".form-group").addClass("has-error");
            $(this).focus();
        }
    });
});

jQuery(function ($) {
    $(".metso-cpf").mask("999.999.999-99", { completed: function () {
        $(this).parent(".form-group").removeClass("has-error")
        if (validarCPF(this.val()) == false) {
            $(this).parent(".form-group").addClass("has-error");
        }
    }
    });

    $('.metso-cpf').on('blur', function (e) {
        $(this).parent(".form-group").removeClass("has-error")
        if (validarCPF($(this).val()) == false && $(this).val().length > 0) {
            $(this).parent(".form-group").addClass("has-error");
            $(this).focus();
        }
    });
});

jQuery(function ($) {
    $(".metso-cnpj").mask("99.999.999/9999-99", { completed: function () {
        $(this).parent(".form-group").removeClass("has-error")
        if (validarCNPJ(this.val()) == false) {
            $(this).parent(".form-group").addClass("has-error");
        }
    }
    });

    $('.metso-cnpj').on('blur', function (e) {
        $(this).parent(".form-group").removeClass("has-error")
        if (validarCNPJ($(this).val()) == false && $(this).val().length > 0) {
            $(this).parent(".form-group").addClass("has-error");
            $(this).focus();
        }
    });
});


//-----------------------------------------------------------------------------------------//
//----------------------------------> GENERAL FUNCTIONS <----------------------------------//
//-----------------------------------------------------------------------------------------//

function ShowOrHide(ElementName, Effect) {
    if ($("#" + ElementName).css("display") == "none" || $("#" + ElementName).css("display") == "hidden") {
        if ($("#" + ElementName).hasClass("hide")) {
            $("#" + ElementName).removeClass("hide");
        }
        $("#" + ElementName).show(Effect);
    } else {
        $("#" + ElementName).hide(Effect);
    }
};

function validarEmail(email) {
    var re = /^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return re.test(email);
}

function validarData(data) {
    var dia = data.substring(0, 2);
    var mes = data.substring(3, 5);
    var ano = data.substring(6, 10);

    if (ano < 1900 || ano > 2050) {
        return false;
    } else if (mes > 12) {
        return false;
    } else if (mes == 1 || mes == 3 || mes == 5 || mes == 7 || mes == 8 || mes == 10 || mes == 12) {
        if (dia > 31) {
            return false
        }
    } else if (mes == 4 || mes == 6 || mes == 9 || mes == 11) {
        if (dia > 30) {
            return false
        }
    } else if (mes == 2) {
        if (dia > 29) {
            return false
        }
    };

    if (data.length > 10) {
        var hora = data.substring(11, 13);
        var minuto = data.substring(14, 16);

        if (hora > 24) {
            return false;
        } else if (minuto > 59) {
            return false;
        }
    };

    return true;
}

function validarCPF(cpf) {
    cpf = cpf.replace(/[^\d]+/g, '');

    if (cpf == '') {
        return false;
    }
    // Elimina CPFs invalidos conhecidos
    if (cpf.length != 11) {
        return false;
    }
    if (cpf == "00000000000" || cpf == "11111111111" ||
        cpf == "22222222222" || cpf == "33333333333" ||
        cpf == "44444444444" || cpf == "55555555555" ||
        cpf == "66666666666" || cpf == "77777777777" ||
        cpf == "88888888888" || cpf == "99999999999") {
        return false;
    }
    // Valida 1o digito 
    add = 0;
    for (i = 0; i < 9; i++) {
        add += parseInt(cpf.charAt(i)) * (10 - i); rev = 11 - (add % 11);
    }
    if (rev == 10 || rev == 11) {
        rev = 0;
    }
    if (rev != parseInt(cpf.charAt(9))) {
        return false;
    }

    // Valida 2o digito 
    add = 0;
    for (i = 0; i < 10; i++) {
        add += parseInt(cpf.charAt(i)) * (11 - i); rev = 11 - (add % 11);
    }
    if (rev == 10 || rev == 11) {
        rev = 0;
    }
    if (rev != parseInt(cpf.charAt(10))) {
        return false;
    }
    return true;
}

function validarCNPJ(cnpj) {

    cnpj = cnpj.replace(/[^\d]+/g, '');

    if (cnpj == '') { return false; }

    if (cnpj.length != 14) {
        return false;
    }

    // Elimina CNPJs invalidos conhecidos
    if (cnpj == "00000000000000" || cnpj == "11111111111111" ||
        cnpj == "22222222222222" || cnpj == "33333333333333" ||
        cnpj == "44444444444444" || cnpj == "55555555555555" ||
        cnpj == "66666666666666" || cnpj == "77777777777777" ||
        cnpj == "88888888888888" || cnpj == "99999999999999") {
        return false;
    }

    // Valida DVs
    tamanho = cnpj.length - 2
    numeros = cnpj.substring(0, tamanho);
    digitos = cnpj.substring(tamanho);
    soma = 0;
    pos = tamanho - 7;
    for (i = tamanho; i >= 1; i--) {
        soma += numeros.charAt(tamanho - i) * pos--;
        if (pos < 2) {
            pos = 9;
        }
    }
    resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
    if (resultado != digitos.charAt(0)) {
        return false;
    }

    tamanho = tamanho + 1;
    numeros = cnpj.substring(0, tamanho);
    soma = 0;
    pos = tamanho - 7;
    for (i = tamanho; i >= 1; i--) {
        soma += numeros.charAt(tamanho - i) * pos--;
        if (pos < 2) {
            pos = 9;
        }
    }
    resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
    if (resultado != digitos.charAt(1)) {
        return false;
    }

    return true;
}


//-----------------------------------------------------------------------------------------//
//--------------------------------------> INDEX PAGE <-------------------------------------//
//-----------------------------------------------------------------------------------------//

var htmlSubMenu;
var htmlSubMenuLateral;

function LoadMenu() {
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + 'Menu/LoadTopMenu',
        data: { pOnlyReleased: true },
        dataType: 'json',
        success: function (data) {
            if (data.Success == true) {
                if (data.idMenu != null) {
                    for (var i = 0; i < data.idMenu.length; i++) {
                        if (data.QtdSubMenu[i] == 0) {
                            $("#ulMenuLateral").append("<li id='lateral" + data.DescricaoMenu[i].replace(" ", "").replace(" ", "").replace(" ", "") + "' onclick='window.location=&#39;" + data.Link[i] + "&#39;'>" + data.DescricaoMenu[i] + " <span class='glyphicon glyphicon-" + data.Icone[i] + "'></span></li>");
                            $("#ulMenu").append("<li><a id='menu" + data.DescricaoMenu[i].replace(" ", "").replace(" ", "").replace(" ", "") + "' href='" + data.Link[i] + "'><span class='glyphicon glyphicon-" + data.Icone[i] + "'></span>&nbsp; " + data.DescricaoMenu[i] + "</a></li>");
                        }
                        else {
                            htmlSubMenu = "";
                            htmlSubMenuLateral = "";
                            LoadSubMenu(data.idMenu[i], data.DescricaoMenu[i]);

                            $("#ulMenuLateral").append("<li onclick='showSubMenu(&#39;" + data.DescricaoMenu[i] + "&#39;)'>" + data.DescricaoMenu[i] + " <span class='glyphicon glyphicon-chevron-down'></span></li>");
                            $("#ulMenuLateral").append(htmlSubMenuLateral);

                            $("#ulMenu").append("<li class='dropdown'> " +
                                                "   <a class='dropdown-toggle' data-toggle='dropdown'><span class='glyphicon glyphicon-" + data.Icone[i] + "'></span>&nbsp; " + data.DescricaoMenu[i] + " <b class='caret'></b></a> " +
                                                "   <ul class='dropdown-menu'> " + htmlSubMenu +
                                                "   </ul>" +
                                                "</li>");
                        }
                    };
                };
            }
            else {
                $("#divConfirmacaoSalvar").addClass("hide");
                $("#txtErrorMessage").text(data.Message);
                $("#divFalhaSalvar").removeClass("hide");
                var timer = window.setTimeout(function () {
                    $("#divFalhaSalvar").addClass("hide");
                    window.clearTimeout(timer);
                }, 5000);
            }

            $(".submenu").hide();
            $("#imgLoadingMenu").hide();
        }
    });
}

function LoadSubMenu(idMenu, MenuDesc) {
    $.ajax({
        async: false,
        cache: false,
        type: 'GET',
        url: url + '/Menu/LoadSubMenu',
        data: { pIdMenu: 0, pIdMenuParent: idMenu, pOnlyReleased: true },
        dataType: 'json',
        success: function (data) {
            if (data.Success == true) {
                for (var i = 0; i < data.idMenu.length; i++) {
                    htmlSubMenuLateral += "<li class='submenu " + MenuDesc + "' onclick='window.location=&#39;" + data.Link[i] + "&#39;'>" + data.DescricaoMenu[i] + "</li> ";
                    htmlSubMenu += "<li><a href='" + data.Link[i] + "'><span class='glyphicon glyphicon-" + data.Icone[i] + "'></span>&nbsp; " + data.DescricaoMenu[i] + "</a></li> ";
                }
            }
            else {
                $("#divConfirmacaoSalvar").addClass("hide");
                $("#txtErrorMessage").text(data.Message);
                $("#divFalhaSalvar").removeClass("hide");
                var timer = window.setTimeout(function () {
                    $("#divFalhaSalvar").addClass("hide");
                    window.clearTimeout(timer);
                }, 5000);
            }
        }
    });
}

function showSubMenu(menu) {
    if ($("." + menu).css("display") == "none") {
        $("." + menu).slideDown();
    } else {
        $("." + menu).slideUp();
    }
}

function setHfDropDownValue(dropDown) {
    var hfName = dropDown.id.replace("ddl", "");
    $("#" + hfName).val($("#" + dropDown.id + " option:selected").val());
}

function setHfChkValue(checkbox) {
    var hfName = checkbox.id.replace("chk", "");
    $("#" + hfName).val($(checkbox).is(":checked"));
}

function confirmLinkNotes(name) {
    if ($.trim($('#' + name).val()) == '') {
        jAlert('Informe o valor do campo', 'Link');
    }
    else if ($('#' + name).val().indexOf("notes://") == -1) {
        jAlert('O link informado não é válido', 'Link');
        $('#' + name).val("");
    }
    else {
        $('#' + name).attr('readonly', 'readonly');
        $('#btnConfirmLink' + name).hide();
        $('#btnOpenLink' + name).show();
        $('#btnRemoveLink' + name).show();
    }
}

function removeLinkNotes(name) {
    $('#' + name).removeAttr('readonly');
    $('#' + name).val("");
    $('#btnConfirmLink' + name).show();
    $('#btnOpenLink' + name).hide();
    $('#btnRemoveLink' + name).hide();
}

function openLinkNotes(name) {
    window.open($('#' + name).val());
}


function onlyInt(evt) {
    evt = (evt) ? evt : window.event
    var charCode = (evt.which) ? evt.which : evt.keyCode
    if (charCode > 31 && (charCode < 48 || charCode > 57)) {
        return false
    }
    return true
}


function setMaskDate(object) {
    if (object.value = '01/01/0001 00:00:00') {
        object.value = '';
    }
    $('#' + object).mask("99/99/9999", { placeholder: "_" });
}

function setMaskDateTime(object) {
    if (object.value = '01/01/0001 00:00:00') {
        object.value = '';
    }
    $('#' + object).mask("99/99/9999? 99:99", { placeholder: "_" });
}

function setMaskTime(object) {
    $('#' + object).mask("99:99", { placeholder: "_" });
}

function setMaskPhone(object) {
    $('#' + object).mask("(99) 9999-9999? 999", { placeholder: "_" });
}

function setMaskPlaca(object) {
    //$('#' + object).mask("aaa-9999", { placeholder: "_" });
}

function setMaskCnpj(object) {
    $('#' + object).mask("99.999.999/9999-99", { placeholder: "_" });
}

function setMaskCpf(object) {
    $('#' + object).mask("999.999.999-99", { placeholder: "_" });
}

function setMaskNcm(object) {
    $('#' + object).mask("9999.99.99", { placeholder: "_" });
}


function MascaraMoedaReal(objTextBox, e) {
    var sep = 0;
    var key = '';
    var i = j = 0;
    var len = len2 = 0;
    var strCheck = '0123456789';
    var aux = aux2 = '';
    //var whichCode = (window.Event) ? e.which : e.keyCode;
    var whichCode = e.which || e.keyCode;
    if (whichCode == 13) return true;
    key = String.fromCharCode(whichCode); // Valor para o código da Chave
    if (strCheck.indexOf(key) == -1) return false; // Chave inválida
    len = objTextBox.value.length;
    for (i = 0; i < len; i++)
        if ((objTextBox.value.charAt(i) != '0') && (objTextBox.value.charAt(i) != ",")) break;
    aux = '';
    for (; i < len; i++)
        if (strCheck.indexOf(objTextBox.value.charAt(i)) != -1) aux += objTextBox.value.charAt(i);
    aux += key;
    len = aux.length;
    //    if (len == 0) objTextBox.value = 'R$ ';
    //    if (len == 1) objTextBox.value = 'R$ ' + "," + '' + aux;
    //    if (len == 2) objTextBox.value = 'R$ ' + "," + aux;
    if (len == 0) objTextBox.value = '';
    if (len == 1) objTextBox.value = '' + "," + '' + aux;
    if (len == 2) objTextBox.value = '' + "," + aux;
    if (len > 2) {
        aux2 = '';
        for (j = 0, i = len - 3; i >= 0; i--) {
            if (j == 3) {
                //aux2 += ".";
                j = 0;
            }
            aux2 += aux.charAt(i);
            j++;
        }
        //objTextBox.value = 'R$ ';
        objTextBox.value = '';
        len2 = aux2.length;
        for (i = len2 - 1; i >= 0; i--)
            objTextBox.value += aux2.charAt(i);
        objTextBox.value += "," + aux.substr(len - 2, len);
    }
    return false;
}

function confirmSubmit(msg) {
    var agree = confirm(msg);
    if (agree)
        return true;
    else
        return false;
}

function IsEmail(email) {
    var exclude = /[^@\-\.\w]|^[_@\.\-]|[\._\-]{2}|[@\.]{2}|(@)[^@]*\1/;
    var check = /@[\w\-]+\./;
    var checkend = /\.[a-zA-Z]{2,3}$/;
    if (((email.search(exclude) != -1) || (email.search(check)) == -1) || (email.search(checkend) == -1)) { return false; }
    else { return true; }
}

function clearFileInput(fupName) {
    var oldInput = document.getElementById(fupName);

    var newInput = document.createElement("input");

    newInput.type = "file";
    newInput.id = oldInput.id;
    newInput.name = oldInput.name;
    newInput.className = oldInput.className;
    newInput.style.cssText = oldInput.style.cssText;

    oldInput.parentNode.replaceChild(newInput, oldInput);
}

function validarCNPJ(cnpj) {

    cnpj = cnpj.replace(/[^\d]+/g, '');

    if (cnpj == '') return false;

    if (cnpj.length != 14)
        return false;

    // Elimina CNPJs invalidos conhecidos
    if (cnpj == "00000000000000" ||
        cnpj == "11111111111111" ||
        cnpj == "22222222222222" ||
        cnpj == "33333333333333" ||
        cnpj == "44444444444444" ||
        cnpj == "55555555555555" ||
        cnpj == "66666666666666" ||
        cnpj == "77777777777777" ||
        cnpj == "88888888888888" ||
        cnpj == "99999999999999")
        return false;

    // Valida DVs
    tamanho = cnpj.length - 2
    numeros = cnpj.substring(0, tamanho);
    digitos = cnpj.substring(tamanho);
    soma = 0;
    pos = tamanho - 7;
    for (i = tamanho; i >= 1; i--) {
        soma += numeros.charAt(tamanho - i) * pos--;
        if (pos < 2)
            pos = 9;
    }
    resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
    if (resultado != digitos.charAt(0))
        return false;

    tamanho = tamanho + 1;
    numeros = cnpj.substring(0, tamanho);
    soma = 0;
    pos = tamanho - 7;
    for (i = tamanho; i >= 1; i--) {
        soma += numeros.charAt(tamanho - i) * pos--;
        if (pos < 2)
            pos = 9;
    }
    resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
    if (resultado != digitos.charAt(1))
        return false;

    return true;

}


function InserirMascaraReal(value) {

    if (value.length == 0 || $.trim(value) == "") {
        return "";
    }

    var inteiros = value.split('.')[0];
    var decimal = "00";
    var valorAux = "";
    var valorFinal = "";

    if (value.split('.').length > 1) {
        decimal = value.split('.')[1];
    }

    cont = 0;
    for (var i = inteiros.length; i > 0; i--) {

        if (cont == 3) {
            valorAux += ".";
            cont = -1;
        }

        cont++;

        valorAux += inteiros.substring(i - 1, i);
    }

    for (var i = valorAux.length; i > 0 ; i--) {
        valorFinal += valorAux.substring(i - 1, i);
    }

    return "R$ " + valorFinal + "," + decimal;
}

function RemoverMascaraReal(value) {

    if (value.length == 0 || $.trim(value) == "") {
        return "";
    }

    value = value.Replace("R$", "").Trim();
    value = value.replace(".", "");
    value = value.replace(",", ".");

    return value;
}

function RemoverMascaraRealParaGravar(value) {

    if (value.length == 0 || $.trim(value) == "") {
        return "";
    }

    value = value.Replace("R$", "").Trim();
    value = value.replace(".", "");

    return value;
}

function maskReal(txt, evt, limit) {
    evt = (evt) ? evt : window.event
    var charCode = (evt.which) ? evt.which : evt.keyCode

    if (onlyInt(evt) || (charCode == 44 && txt.value.length > 0 && txt.value.indexOf(',') == -1)) {
        if (limit != null && txt.value.indexOf(',') != -1 && txt.value.split(',')[1].length == limit) {
            return false;
        }
        return true;
    }

    return false;
}


///Funções adicionadas por Miguel

function validarCNPJ(cnpj) {

    cnpj = cnpj.replace(/[^\d]+/g, '');

    if (cnpj == '') { return false; }

    if (cnpj.length != 14) {
        return false;
    }

    // Elimina CNPJs invalidos conhecidos
    if (cnpj == "00000000000000" || cnpj == "11111111111111" ||
        cnpj == "22222222222222" || cnpj == "33333333333333" ||
        cnpj == "44444444444444" || cnpj == "55555555555555" ||
        cnpj == "66666666666666" || cnpj == "77777777777777" ||
        cnpj == "88888888888888" || cnpj == "99999999999999") {
        return false;
    }

    // Valida DVs
    tamanho = cnpj.length - 2
    numeros = cnpj.substring(0, tamanho);
    digitos = cnpj.substring(tamanho);
    soma = 0;
    pos = tamanho - 7;
    for (i = tamanho; i >= 1; i--) {
        soma += numeros.charAt(tamanho - i) * pos--;
        if (pos < 2) {
            pos = 9;
        }
    }
    resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
    if (resultado != digitos.charAt(0)) {
        return false;
    }

    tamanho = tamanho + 1;
    numeros = cnpj.substring(0, tamanho);
    soma = 0;
    pos = tamanho - 7;
    for (i = tamanho; i >= 1; i--) {
        soma += numeros.charAt(tamanho - i) * pos--;
        if (pos < 2) {
            pos = 9;
        }
    }
    resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
    if (resultado != digitos.charAt(1)) {
        return false;
    }

    return true;
}


function ShowLoading(title, content) {
    $("#MsgBoxBack").show();
    $.SmartMessageBox({
        title: title,
        content: content,
        buttons: ''
    })
}

function HideLoading() {
    $(".divMessageBox").hide();
    $("#divSapLogon").hide();
}

function HideLoadingWarning() {
    ExistMsg = 0;
    setTimeout(function () {
        $("#MsgBoxBack").hide();
    }, 500);
    SmartUnLoading();
}

function showAlertMessage(title, content) {
    ExistMsg = 0;
    $.SmartMessageBox({
        title: title,
        content: content,
        buttons: '[OK]'
    }, function (ButtonPressed) {
        SmartUnLoading();
    });
}