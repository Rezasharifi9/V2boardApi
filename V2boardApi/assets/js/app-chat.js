/**
 * App Chat
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
    (function () {
        const chatContactsBody = document.querySelector('.app-chat-contacts .sidebar-body'),
            chatContactListItems = [].slice.call(
                document.querySelectorAll('.chat-contact-list-item:not(.chat-contact-list-item-title)')
            ),
            chatHistoryBody = document.querySelector('.chat-history-body'),
            chatSidebarLeftBody = document.querySelector('.app-chat-sidebar-left .sidebar-body'),
            chatSidebarRightBody = document.querySelector('.app-chat-sidebar-right .sidebar-body'),
            chatUserStatus = [].slice.call(document.querySelectorAll(".form-check-input[name='chat-user-status']")),
            chatSidebarLeftUserAbout = $('.chat-sidebar-left-user-about'),
            formSendMessage = document.querySelector('.form-send-message'),
            messageInput = document.querySelector('.message-input'),
            searchInput = document.querySelector('.chat-search-input'),
            speechToText = $('.speech-to-text'), // ! jQuery dependency for speech to text
            userStatusObj = {
                active: 'avatar-online',
                offline: 'avatar-offline',
                away: 'avatar-away',
                busy: 'avatar-busy'
            };

        // Initialize PerfectScrollbar
        // ------------------------------

        // Chat contacts scrollbar
        if (chatContactsBody) {
            new PerfectScrollbar(chatContactsBody, {
                wheelPropagation: false,
                suppressScrollX: true
            });
        }

        // Chat history scrollbar
        if (chatHistoryBody) {
            new PerfectScrollbar(chatHistoryBody, {
                wheelPropagation: false,
                suppressScrollX: true
            });
        }

        // Sidebar left scrollbar
        if (chatSidebarLeftBody) {
            new PerfectScrollbar(chatSidebarLeftBody, {
                wheelPropagation: false,
                suppressScrollX: true
            });
        }

        // Sidebar right scrollbar
        if (chatSidebarRightBody) {
            new PerfectScrollbar(chatSidebarRightBody, {
                wheelPropagation: false,
                suppressScrollX: true
            });
        }

        // Scroll to bottom function
        function scrollToBottom() {
            chatHistoryBody.scrollTo(0, chatHistoryBody.scrollHeight);
        }
        scrollToBottom();

        // User About Maxlength Init
        if (chatSidebarLeftUserAbout.length) {
            chatSidebarLeftUserAbout.maxlength({
                alwaysShow: true,
                warningClass: 'label label-success bg-success text-white',
                limitReachedClass: 'label label-danger',
                separator: '/',
                validate: true,
                threshold: 120
            });
        }

        // Update user status
        chatUserStatus.forEach(el => {
            el.addEventListener('click', e => {
                let chatLeftSidebarUserAvatar = document.querySelector('.chat-sidebar-left-user .avatar'),
                    value = e.currentTarget.value;
                //Update status in left sidebar user avatar
                chatLeftSidebarUserAvatar.removeAttribute('class');
                Helpers._addClass('avatar avatar-xl ' + userStatusObj[value] + '', chatLeftSidebarUserAvatar);
                //Update status in contacts sidebar user avatar
                let chatContactsUserAvatar = document.querySelector('.app-chat-contacts .avatar');
                chatContactsUserAvatar.removeAttribute('class');
                Helpers._addClass('flex-shrink-0 avatar ' + userStatusObj[value] + ' me-3', chatContactsUserAvatar);
            });
        });

        // Select chat or contact
        chatContactListItems.forEach(chatContactListItem => {
            // Bind click event to each chat contact list item
            chatContactListItem.addEventListener('click', e => {
                // Remove active class from chat contact list item
                chatContactListItems.forEach(chatContactListItem => {
                    chatContactListItem.classList.remove('active');
                });
                // Add active class to current chat contact list item
                e.currentTarget.classList.add('active');
            });
        });

        // Filter Chats
        if (searchInput) {
            searchInput.addEventListener('keyup', e => {
                let searchValue = e.currentTarget.value.toLowerCase(),
                    searchChatListItemsCount = 0,
                    searchContactListItemsCount = 0,
                    chatListItem0 = document.querySelector('.chat-list-item-0'),
                    contactListItem0 = document.querySelector('.contact-list-item-0'),
                    searchChatListItems = [].slice.call(
                        document.querySelectorAll('#chat-list li:not(.chat-contact-list-item-title)')
                    ),
                    searchContactListItems = [].slice.call(
                        document.querySelectorAll('#contact-list li:not(.chat-contact-list-item-title)')
                    );

                // Search in chats
                searchChatContacts(searchChatListItems, searchChatListItemsCount, searchValue, chatListItem0);
                // Search in contacts
                searchChatContacts(searchContactListItems, searchContactListItemsCount, searchValue, contactListItem0);
            });
        }

        // Search chat and contacts function
        function searchChatContacts(searchListItems, searchListItemsCount, searchValue, listItem0) {
            searchListItems.forEach(searchListItem => {
                let searchListItemText = searchListItem.textContent.toLowerCase();
                if (searchValue) {
                    if (-1 < searchListItemText.indexOf(searchValue)) {
                        searchListItem.classList.add('d-flex');
                        searchListItem.classList.remove('d-none');
                        searchListItemsCount++;
                    } else {
                        searchListItem.classList.add('d-none');
                    }
                } else {
                    searchListItem.classList.add('d-flex');
                    searchListItem.classList.remove('d-none');
                    searchListItemsCount++;
                }
            });
            // Display no search fount if searchListItemsCount == 0
            if (searchListItemsCount == 0) {
                listItem0.classList.remove('d-none');
            } else {
                listItem0.classList.add('d-none');
            }
        }

        // Send Message
        formSendMessage.addEventListener('submit', e => {
            e.preventDefault();
            if (messageInput.value) {


                var li = document.createElement('li');
                li.className = 'chat-message chat-message-right';

                var flex = document.createElement('div');
                flex.className = "d-flex overflow-hidden";

                var div2 = document.createElement('div');
                div2.className = 'chat-message-wrapper flex-grow-1';

                var div3 = document.createElement('div');
                div3.className = 'user-avatar flex-shrink-0 ms-3';

                var div4 = document.createElement('div');
                div4.className = 'avatar avatar-sm';

                var img = document.createElement('img');
                img.className = 'rounded-circle';
                img.src = '../../assets/img/avatars/profile.png';


                let text_metued = document.createElement('div');
                text_metued.className = 'text-end text-muted mt-1';
                let time = document.createElement('div');
                let childTime = document.createElement('small');

                const currentDate = new Date();
                const hours = currentDate.getHours(); // ساعت
                const minutes = currentDate.getMinutes(); // دقیقه

                childTime.textContent = hours + ":" + minutes;

                time.className = 'ti ti-checks ti-xs me-1 text-secoundery';
                
                text_metued.appendChild(time);
                text_metued.appendChild(childTime);
                // Create a div and add a class
                let renderMsg = document.createElement('div');
                renderMsg.className = 'chat-message-text mt-2';
                renderMsg.innerHTML = '<p class="mb-0 text-break">' + messageInput.value + '</p>';
                div2.appendChild(renderMsg);
                div2.appendChild(text_metued);

                div4.appendChild(img);
                div3.appendChild(div4);
                flex.appendChild(div2);
                flex.appendChild(div3);
                li.appendChild(flex);
                debugger;
                var userid = $(".chat-contact-list-item.active").attr("data-id");

                var chatSelect = $(".classChat_" + userid);

                $(chatSelect).append(li);

                scrollToBottom();

               


                // برای ارسال پیام از فرستنده به گیرنده
                sendMessageToUser(messageInput.value, userid);
                messageInput.value = '';
            }
        });

        // on click of chatHistoryHeaderMenu, Remove data-overlay attribute from chatSidebarLeftClose to resolve overlay overlapping issue for two sidebar
        let chatHistoryHeaderMenu = document.querySelector(".chat-history-header [data-target='#app-chat-contacts']"),
            chatSidebarLeftClose = document.querySelector('.app-chat-sidebar-left .close-sidebar');
        chatHistoryHeaderMenu.addEventListener('click', e => {
            chatSidebarLeftClose.removeAttribute('data-overlay');
        });
        // }

        // Speech To Text
        if (speechToText.length) {
            var SpeechRecognition = SpeechRecognition || webkitSpeechRecognition;
            if (SpeechRecognition !== undefined && SpeechRecognition !== null) {
                var recognition = new SpeechRecognition(),
                    listening = false;
                speechToText.on('click', function () {
                    const $this = $(this);
                    recognition.onspeechstart = function () {
                        listening = true;
                    };
                    if (listening === false) {
                        recognition.start();
                    }
                    recognition.onerror = function (event) {
                        listening = false;
                    };
                    recognition.onresult = function (event) {
                        $this.closest('.form-send-message').find('.message-input').val(event.results[0][0].transcript);
                    };
                    recognition.onspeechend = function (event) {
                        listening = false;
                        recognition.stop();
                    };
                });
            }
        }


        var chatHub = $.connection.hub.proxies.chathub;

        function sendMessageToUser(message, receiverId) {

            chatHub.server.sendMessage(message, receiverId);
        }

        // دریافت پیام از سرور
        chatHub.client.broadcastMessage = function (message) {


            var li = document.createElement('li');
            li.className = 'chat-message';

            var flex = document.createElement('div');
            flex.className = "d-flex overflow-hidden";

            var div2 = document.createElement('div');
            div2.className = 'chat-message-wrapper flex-grow-1';

            var div3 = document.createElement('div');
            div3.className = 'user-avatar flex-shrink-0 ms-3 me-3';

            var div4 = document.createElement('div');
            div4.className = 'avatar avatar-sm';

            var img = document.createElement('img');
            img.className = 'rounded-circle';
            img.src = '../../assets/img/avatars/profile.png';




            // Create a div and add a class
            let renderMsg = document.createElement('div');
            renderMsg.className = 'chat-message-text';
            renderMsg.innerHTML = '<p class="mb-0">' + message + '</p>';


            let text_metued = document.createElement('div');
            text_metued.className = 'text-end text-muted mt-1';
            let childTime = document.createElement('small');

            const currentDate = new Date();
            const hours = currentDate.getHours(); // ساعت
            const minutes = currentDate.getMinutes(); // دقیقه

            childTime.textContent = hours + ":" + minutes;



            div2.appendChild(renderMsg);
            div2.appendChild(text_metued);

            div4.appendChild(img);
            div3.appendChild(div4);
            flex.appendChild(div3);
            flex.appendChild(div2);

            li.appendChild(flex);

            document.querySelector('.chat-history').append(li);

            scrollToBottom();



            var userid = $(".chat-contact-list-item.active").attr("data-id");

            chatHub.server.seen(userid);
        };

        chatHub.client.seenMessage = function ()
        {
            $(".ti.ti-checks.ti-xs.me-1").removeClass('text-secoundery');
            $(".ti.ti-checks.ti-xs.me-1").addClass('text-success');
        }


    })();
});

