var buttonGroups,list=document.querySelectorAll(".team-list");

function onButtonGroupClick(e){"list-view-button"===e.target.id||"list-view-button"===e.target.parentElement.id?(document.getElementById("list-view-button").classList.add("active"),document.getElementById("grid-view-button").classList.remove("active"),Array.from(list).forEach(function(e){e.classList.add("list-view-filter"),e.classList.remove("grid-view-filter")})):(document.getElementById("grid-view-button").classList.add("active"),document.getElementById("list-view-button").classList.remove("active"),Array.from(list).forEach(function(e){e.classList.remove("list-view-filter"),e.classList.add("grid-view-filter")}))}list&&(buttonGroups=document.querySelectorAll(".filter-button"))&&Array.from(buttonGroups).forEach(
    function(e){e.addEventListener("click",onButtonGroupClick)});

    function openmodal (btn) {
            
        switch (btn.attributes["modal-type"].value) {
            case "close_all":
                $('#modal-title').html("ยืนยันการปิดระบบกระจายเสียงทั้งหมด");
                $('#modal-title').attr('class', 'modal-title text-danger');                
                $('#modal-content').html("ต้องการ \"ปิด\" ระบบกระจายเสียงทั้งหมดใช่หรือไม่");
                $('#modal-confirm-btn').text("\"ปิด\" ระบบกระจายเสียงทั้งหมด");
                $('#modal-confirm-btn').attr('onclick', 'close_all()');
                break;
            case "open_all":
                $('#modal-title').html("ยืนยันการเปิดระบบกระจายเสียงทั้งหมด");              
                $('#modal-title').attr('class', 'modal-title text-primary'); 
                $('#modal-content').html("ต้องการ \"เปิด\" ระบบกระจายเสียงทั้งหมดใช่หรือไม่"); 
                $('#modal-confirm-btn').text("\"เปิด\" ระบบกระจายเสียงทั้งหมด");
                $('#modal-confirm-btn').attr('onclick', 'openbroadcast(true)');
                break;
            case "mute_all":
                $('#modal-title').html("ยืนยันการปิดเสียงทั้งหมด");
                $('#modal-title').attr('class', 'modal-title text-warning');
                $('#modal-content').html("ต้องการ \"ปิดเสียง\" ทั้งหมดใช่หรือไม่");
                $('#modal-confirm-btn').text("\"ปิดเสียง\" ทั้งหมด");
                $('#modal-confirm-btn').attr('onclick', 'mute_all(true)');
                btn.attributes["modal-type"].value = "unmute_all";
                btn.innerHTML = "เปิดเสียงทั้งหมด";
                break;
            case "unmute_all":
                $('#modal-title').html("ยืนยันการเปิดเสียงทั้งหมด");
                $('#modal-title').attr('class', 'modal-title text-success');
                $('#modal-content').html("ต้องการ \"เปิดเสียง\" ทั้งหมดใช่หรือไม่");
                $('#modal-confirm-btn').text("\"เปิดเสียง\" ทั้งหมด");
                $('#modal-confirm-btn').attr('onclick', 'mute_all(false)');
                btn.attributes["modal-type"].value = "mute_all";
                btn.innerHTML = "ปิดเสียงทั้งหมด";
                break;
            case "close":
                $('#modal-title').html("ยืนยันการปิดระบบกระจายเสียง");
                $('#modal-title').attr('class', 'modal-title text-danger');
                $('#modal-content').html(`ต้องการ \"ปิด\" ระบบกระจายเสียง ${document.getElementById(`option-${btn.attributes["value"].value}`).innerHTML}`);
                $('#modal-confirm-btn').text(`\"ปิด\" ${document.getElementById(`option-${btn.attributes["value"].value}`).innerHTML}`);
                $('#modal-confirm-btn').attr('onclick', 'kick(\"' + btn.attributes["value"].value +'\")');
            }          
        
    }

    function filterTable(event) {
        const searchText = event.target.value.toLowerCase();
        const userContainers = document.querySelectorAll('[id^="user-container-"]');
    
        userContainers.forEach(container => {
            const name = container.querySelector('.member-name h5').textContent.toLowerCase();
    
            if (name.includes(searchText)) {
                container.style.display = '';
            } else {
                container.style.display = 'none';
            }
        });
    }