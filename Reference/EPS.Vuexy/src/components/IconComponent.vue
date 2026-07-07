<template>
    <span class="icon-wrapper" v-html="getIconSvg()"></span>
</template>

<script>
/* eslint-disable */
export default {
    name: 'IconComponent',
    props: {
        icon: {
            type: String,
            required: true,
            validator: (value) => {
                // Tách tên icon từ định dạng 'tên-bộ-sưu-tập:tên-icon'
                const iconName = value.split(':').pop()
                return [
                    'alert-twotone-loop',
                    'map-marker-alt',
                    'security-lock',
                    'buildings-2-outline',
                    'notification-unread-lines-broken',
                    'screencast-broken',
                    'calendar2-event',
                    'prohibition',
                    'list',
                    'cctv',
                    'gui-management',
                    'group',
                    'user',
                    'home-1-line',
                    'more-2-line',
                    'add',
                    'camera',
                    'close',
                    'screenshot-monitor-outline-rounded',
                    'visibility',
                    'setting',
                    'enter-outline',
                    'cog-filled-loop',
                    'eye-outline',
                    'notification-settings-outline',
                    'access',
                    'delete-20-regular',
                    'camera-cog',
                    'dashboard',
                    'shipping-truck',
                    'document-report',
                    'clipboard-list',
                    'sunny-filled-loop-to-moon-alt-filled-loop-transition',
                    'moon-filled-to-sunny-filled-loop-transition',
                    'account',
                    'clipboard-list',
                    'account',
                    'department-line',
                    'general-contractor',
                    'app-users-roles',
                    'device-line',
                    'avg-time-outline',
                    'permissions',
                    'warning-outlined',
                    'vehicle',
                    'employee-group-line',
                    'rocket',
                    'fire',
                    'multiple-users',
                    'water-level',
                    'user-protection-2',
                    'briefcase-conveyor-belt',
                    'traffic-outline',
                    'plus-thick',
                    'plus-box',
                    'minus-box',
                    'location',
                    'save-outline',
                    'edit-twotone',
                    'arrow-small-left',
                    'cancel',
                    'round-map',
                    'play-twotone',
                    'delete',
                ].includes(iconName)
            },
        },
    },
    data() {
        return {
            icons: {
                // NEW: line-md:play-twotone
                'play-twotone':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">' +
                    '<g fill="currentColor">' +
                    '<path fill-opacity=".35" d="M12 2a10 10 0 1 0 0 20a10 10 0 0 0 0-20z"/>' +
                    '<path d="M10 8.5v7L16 12z"/>' +
                    '</g>' +
                    '</svg>',

                // NEW: material-symbols:delete
                delete:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">' +
                    '<path fill="currentColor" d="M7 21h10a2 2 0 0 0 2-2V7H5v12a2 2 0 0 0 2 2zm10-16V3h-3.5l-1-1h-3l-1 1H5v2h12zM9 9h2v8H9V9zm4 0h2v8h-2V9z"/>' +
                    '</svg>',
                'clipboard-list':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M16 6l2.29 2.29l-4.88 4.88l-4-4L11.7 6zm-2 7.41L17.71 9.7l-1.42-1.42L12 12.59L7.71 8.29L6.29 9.71l4 4zM4 3h6c0-1.11.89-2 2-2s2 .89 2 2h6v2H4V3zm0 4h16v14H4V7z"/></svg>',
                'sunny-filled-loop-to-moon-alt-filled-loop-transition':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a10 10 0 00-7.07 17.07A10 10 0 0012 2zm0 18a8 8 0 110-16 8 8 0 010 16z"><animateTransform attributeName="transform" type="rotate" from="0 12 12" to="360 12 12" dur="1s" repeatCount="indefinite"/></path></svg>',
                'moon-filled-to-sunny-filled-loop-transition':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a10 10 0 00-7.07 17.07A10 10 0 0012 2zm0 18a8 8 0 110-16 8 8 0 010 16z"><animateTransform attributeName="transform" type="rotate" from="360 12 12" to="0 12 12" dur="1s" repeatCount="indefinite"/></path></svg>',
                account:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 4a4 4 0 1 0 0 8a4 4 0 0 0 0-8M6 8a6 6 0 1 1 12 0A6 6 0 0 1 6 8m2 10v1a3 3 0 0 0 3 3h2a3 3 0 0 0 3-3v-1a4 4 0 0 0-8 0"/></svg>',
                'department-line':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a2 2 0 0 1 2 2v2h4a2 2 0 0 1 2 2v4a2 2 0 0 1-2 2h-4v4a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-4H2a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4V4a2 2 0 0 1 2-2zm0 2H8v4H2v4h4v4h6v-4h4V8h-4z"/></svg>',
                'general-contractor':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M19 3h-4.5a2 2 0 0 0-2-2h-3a2 2 0 0 0-2 2H3v18h18V3zm-2 16H5V7h12v12zm-7-9h4v2h-4v-2zm0 4h4v2h-4v-2z"/></svg>',
                'app-users-roles':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-6 2c-2.67 0-8 1.34-8 4v2h12v-2c0-2.66-5.33-4-8-4zm12 0c-2.67 0-8 1.34-8 4v2h12v-2c0-2.66-5.33-4-8-4z"/></svg>',
                'device-line':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-1 16H6V7h12v12z"/></svg>',
                'avg-time-outline':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm1-13h-2v6l5.25 3.15.75-1.23-4.5-2.67z"/></svg>',
                permissions:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a10 10 0 0 0-7.07 17.07A10 10 0 0 0 12 22a10 10 0 0 0 7.07-2.93A10 10 0 0 0 22 12a10 10 0 0 0-10-10zm0 18a8 8 0 0 1-8-8c0-1.72.54-3.31 1.46-4.61l11.15 11.15A7.94 7.94 0 0 1 12 20zm6.54-4.61L7.39 4.24A8 8 0 0 1 20 12a7.94 7.94 0 0 1-1.46 3.39z"/></svg>',
                'warning-outlined':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2L1.45 20.22a1 1 0 0 0 .87 1.5h21.36a1 1 0 0 0 .87-1.5L12 2zm0 2.24L22.55 20H1.45L12 4.24zM11 8h2v6h-2V8zm0 8h2v2h-2v-2z"/></svg>',
                vehicle:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M18.92 5.01C18.72 4.42 18.16 4 17.5 4h-11C5.84 4 5.29 4.42 5.08 5.01L3 11v8c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-1h12v1c0 .55.45 1 1 1h1c.55 0 1-.45 1-1v-8l-2.08-5.99zM6.5 15c-.83 0-1.5-.67-1.5-1.5S5.67 12 6.5 12s1.5.67 1.5 1.5S7.33 15 6.5 15zm11 0c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zM5 10l1.5-4.5h11L19 10H5z"/></svg>',
                'employee-group-line':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg>',
                rocket: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M20 9c0-4.97-5.22-8.47-9.06-6.36C7.1 4.74 3 9.96 3 14c0 1.1.9 2 2 2h3v-4l3 3 3-3v4h3c1.1 0 2-.9 2-2zm-8 3l-3 3h2v3h2v-3h2l-3-3zm0-2a2 2 0 1 0 0-4 2 2 0 0 0 0 4z"/></svg>',
                fire: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 23c3.03 0 5.5-2.47 5.5-5.5 0-2.15-1.24-4.02-3.06-4.94-.45 1.69-1.92 2.94-3.44 2.94-1.52 0-2.98-1.25-3.44-2.94C5.74 13.48 4.5 15.35 4.5 17.5 4.5 20.53 6.97 23 12 23zm0-2c-2.76 0-4.5-1.74-4.5-3.5 0-1.38.88-2.6 2.22-3.11l.78 2.33 1.5-4.5 1.5 4.5.78-2.33c1.34.51 2.22 1.73 2.22 3.11 0 1.76-1.74 3.5-4.5 3.5z"/></svg>',
                'multiple-users':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5s-3 1.34-3 3 1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"/></svg>',
                'water-level':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 3c-4.97 0-9 3.13-9 7 0 2.7 1.94 5.07 4.83 6.28.3-.58.73-1.09 1.27-1.47-.55-.36-1.02-.83-1.39-1.36C5.56 12.46 4 10.94 4 9c0-2.76 3.58-5 8-5s8 2.24 8 5c0 1.94-1.56 3.46-3.71 4.45-.37.53-.84 1-1.39 1.36.54.38.97.89 1.27 1.47C18.06 15.07 20 12.7 20 10c0-3.87-4.03-7-9-7zm0 18c-3.31 0-6-2.24-6-5 0-1.94 1.56-3.46 3.71-4.45.37-.53.84-1 1.39-1.36-.54-.38-.97-.89-1.27-1.47C7.94 9.93 6 12.3 6 15c0 3.87 4.03 7 9 7 1.66 0 3.22-.38 4.5-1.03-.3.58-.73 1.09-1.27 1.47C16.22 21.46 14.66 20 12 20z"/></svg>',
                'user-protection-2':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm0 2.18L19.87 7H4.13L12 3.18zM12 19c-3.31 0-6-3.57-6-7 0-1.94 1.56-3.46 3.71-4.45l.78 2.33 1.5-4.5 1.5 4.5.78-2.33c2.15.99 3.71 2.51 3.71 4.45 0 3.43-2.69 7-6 7z"/></svg>',
                'briefcase-conveyor-belt':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M2 11h3v6H2zm5 0h14c1.1 0 2 .9 2 2v2c0 1.1-.9 2-2 2H7c-1.1 0-2-.9-2-2v-2c0-1.1.9-2 2-2zm0 2v2h14v-2H7zm3-5h4c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2h-4c-1.1 0-2 .9-2 2v2c0 1.1.9 2 2 2zm0-2v-2h4v2h-4z"/></svg>',
                'traffic-outline':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a10 10 0 0 0-10 10c0 5.52 4.48 10 10 10s10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm-1-14h2v2h-2V6zm0 4h2v2h-2v-2zm0 4h2v4h-2v-4z"/></svg>',
                'alert-twotone-loop':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 22c5.523 0 10-4.477 10-10S17.523 2 12 2 2 6.477 2 12s4.477 10 10 10zm0-2a8 8 0 100-16 8 8 0 000 16zM11 7h2v6h-2V7zm0 8h2v2h-2v-2z"/></svg>',
                'map-marker-alt':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5a2.5 2.5 0 010-5 2.5 2.5 0 010 5z"/></svg>',

                'security-lock':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"><path d="M11.998 2C8.99 2 7.04 4.019 4.734 4.755c-.938.3-1.407.449-1.597.66c-.19.21-.245.519-.356 1.135c-1.19 6.596 1.41 12.694 7.61 15.068c.665.255.998.382 1.61.382s.946-.128 1.612-.383c6.199-2.373 8.796-8.471 7.606-15.067c-.111-.616-.167-.925-.357-1.136s-.658-.36-1.596-.659C16.959 4.019 15.006 2 11.998 2"/><path d="M10.337 10.38c-1.08 0-1.62.78-1.74 1.26s-.12 2.22-.048 2.94c.24.9.84 1.272 1.428 1.392c.54.048 2.82.03 3.48.03c.96.018 1.68-.342 1.98-1.422c.06-.36.12-2.34-.03-2.94c-.318-.96-1.11-1.26-1.71-1.26zm-.087-.421c0-.06.008-.406.01-.84c0-.398-.034-.78.156-1.13c.71-1.414 2.75-1.27 3.254.17c.087.237.093.612.09.96c-.003.443.006.84.006.84"/></g></svg>',
                'buildings-2-outline':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M3 21h18v-2H3v2zm4-4h10v-8H7v8zm2-6h2v2H9v-2zm4 0h2v2h-2v-2zm-6-6h12V3H7v2z"/></svg>',
                'notification-unread-lines-broken':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.63-5.64-4.5-6.32V4c0-1.1-.9-2-2-2s-2 .9-2 2v.68C6.63 5.36 5 7.93 5 11v5l-2 2v1h18v-1l-2-2z"/></svg>',
                'screencast-broken':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h9v-2H4V8h16v10h-2v2h2c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2z"/></svg>',
                'calendar2-event':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 16 16"><path fill="currentColor" d="M11 7.5a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5z"/><path fill="currentColor" d="M3.5 0a.5.5 0 0 1 .5.5V1h8V.5a.5.5 0 0 1 1 0V1h1a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h1V.5a.5.5 0 0 1 .5-.5M2 2a1 1 0 0 0-1 1v11a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V3a1 1 0 0 0-1-1z"/><path fill="currentColor" d="M2.5 4a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5H3a.5.5 0 0 1-.5-.5z"/></svg>',
                prohibition:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M19.141 5A9.97 9.97 0 0 0 12 2C6.477 2 2 6.477 2 12a9.97 9.97 0 0 0 2.859 7M19.14 5A9.97 9.97 0 0 1 22 12c0 5.523-4.477 10-10 10a9.97 9.97 0 0 1-7.141-3M19.14 5L4.86 19"/></svg>',
                list: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 32 32"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-width="2" d="M12 8h15m-15 8h9m-9 8h15M7 24a1 1 0 1 1-2 0a1 1 0 0 1 2 0Zm0-8a1 1 0 1 1-2 0a1 1 0 0 1 2 0Zm0-8a1 1 0 1 1-2 0a1 1 0 0 1 2 0Z"/></svg>',
                cctv: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16.75 12h3.632a1 1 0 0 1 .894 1.447l-2.034 4.069a1 1 0 0 1-1.708.134l-2.124-2.97m1.696-5.627a1 1 0 0 1 .447 1.341l-3.106 6.211a1 1 0 0 1-1.342.447L3.61 12.3a2.92 2.92 0 0 1-1.3-3.91L3.69 5.6a2.92 2.92 0 0 1 3.92-1.3zM2 19h3.76a2 2 0 0 0 1.8-1.1L9 15m-7 6v-4m5-8h.01"/></svg>',
                'gui-management':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 32 32"><path fill="currentColor" d="M30 24v-2h-2.101a5 5 0 0 0-.732-1.753l1.49-1.49l-1.414-1.414l-1.49 1.49A5 5 0 0 0 24 18.101V16h-2v2.101a5 5 0 0 0-1.753.732l-1.49-1.49l-1.414 1.414l1.49 1.49A5 5 0 0 0 18.101 22H16v2h2.101a5 5 0 0 0 .732 1.753l-1.49 1.49l1.414 1.414l1.49-1.49a5 5 0 0 0 1.753.732V30h2v-2.101a5 5 0 0 0 1.753-.732l1.49 1.49l1.414-1.414l-1.49-1.49A5 5 0 0 0 27.899 24Zm-7 2a3 3 0 1 1 3-3a3.003 3.003 0 0 1-3 3"/><path fill="currentColor" d="M28 4H4a2 2 0 0 0-2 2v20a2 2 0 0 0 2 2h10v-2H4V12h24v3h2V6a2 2 0 0 0-2-2m0 6H4V6h24Z"/><circle cx="20" cy="8" r="1" fill="currentColor"/><circle cx="23" cy="8" r="1" fill="currentColor"/><circle cx="26" cy="8" r="1" fill="currentColor"/></svg>',
                group: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg>',
                user: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/></svg>',
                'home-1-line':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 3l10 9h-3v8H5v-8H2l10-9zm0 2.83L5.83 11H7v7h10v-7h1.17L12 5.83z"/></svg>',
                'more-2-line':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 8a2 2 0 100-4 2 2 0 000 4zm0 2a2 2 0 100 4 2 2 0 000-4zm0 6a2 2 0 100 4 2 2 0 000-4z"/></svg>',
                add: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>',
                camera: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 16a4 4 0 100-8 4 4 0 000 8zm0 2a6 6 0 110-12 6 6 0 010 12zm0-14H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2h-5l-2-2z"/></svg>',
                close: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12 19 6.41z"/></svg>',
                'screenshot-monitor-outline-rounded':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm-2 14H6V8h12v10z"/></svg>',
                visibility:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zm0 12c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8a3 3 0 100 6 3 3 0 000-6z"/></svg>',
                setting:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M19.9 12.66a1 1 0 0 1 0-1.32l1.28-1.44a1 1 0 0 0 .12-1.17l-2-3.46a1 1 0 0 0-1.07-.48l-1.88.38a1 1 0 0 1-1.15-.66l-.61-1.83a1 1 0 0 0-.95-.68h-4a1 1 0 0 0-1 .68l-.56 1.83a1 1 0 0 1-1.15.66L5 4.79a1 1 0 0 0-1 .48L2 8.73a1 1 0 0 0 .1 1.17l1.27 1.44a1 1 0 0 1 0 1.32L2.1 14.1a1 1 0 0 0-.1 1.17l2 3.46a1 1 0 0 0 1.07.48l1.88-.38a1 1 0 0 1 1.15.66l.61 1.83a1 1 0 0 0 1 .68h4a1 1 0 0 0 .95-.68l.61-1.83a1 1 0 0 1 1.15-.66l1.88.38a1 1 0 0 0 1.07-.48l2-3.46a1 1 0 0 0-.12-1.17ZM18.41 14l.8.9l-1.28 2.22l-1.18-.24a3 3 0 0 0-3.45 2L12.92 20h-2.56L10 18.86a3 3 0 0 0-3.45-2l-1.18.24l-1.3-2.21l.8-.9a3 3 0 0 0 0-4l-.8-.9l1.28-2.2l1.18.24a3 3 0 0 0 3.45-2L10.36 4h2.56l.38 1.14a3 3 0 0 0 3.45 2l1.18-.24l1.28 2.22l-.8.9a3 3 0 0 0 0 3.98m-6.77-6a4 4 0 1 0 4 4a4 4 0 0 0-4-4m0 6a2 2 0 1 1 2-2a2 2 0 0 1-2 2"/></svg>',
                'enter-outline':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 512 512"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="32" d="M176 176v-40a40 40 0 0 1 40-40h208a40 40 0 0 1 40 40v240a40 40 0 0 1-40 40H216a40 40 0 0 1-40-40v-40"/><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="32" d="m272 336l80-80l-80-80M48 256h288"/></svg>',
                'cog-filled-loop':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><mask id="SVGzJbIMdSz"><defs><symbol id="SVGKKfyAcRD"><path d="M11 13L15.74 5.5C16.03 5.67 16.31 5.85 16.57 6.05C16.57 6.05 16.57 6.05 16.57 6.05C16.64 6.1 16.71 6.16 16.77 6.22C18.14 7.34 19.09 8.94 19.4 10.75C19.41 10.84 19.42 10.92 19.43 11C19.43 11 19.43 11 19.43 11C19.48 11.33 19.5 11.66 19.5 12z"><animate fill="freeze" attributeName="d" begin="0.5s" dur="0.2s" values="M11 13L15.74 5.5C16.03 5.67 16.31 5.85 16.57 6.05C16.57 6.05 16.57 6.05 16.57 6.05C16.64 6.1 16.71 6.16 16.77 6.22C18.14 7.34 19.09 8.94 19.4 10.75C19.41 10.84 19.42 10.92 19.43 11C19.43 11 19.43 11 19.43 11C19.48 11.33 19.5 11.66 19.5 12z;M11 13L15.74 5.5C16.03 5.67 16.31 5.85 16.57 6.05C16.57 6.05 19.09 5.04 19.09 5.04C19.25 4.98 19.52 5.01 19.6 5.17C19.6 5.17 21.67 8.75 21.67 8.75C21.77 8.92 21.73 9.2 21.6 9.32C21.6 9.32 19.43 11 19.43 11C19.48 11.33 19.5 11.66 19.5 12z"/></path></symbol></defs><g fill="none" stroke="#fff" stroke-width="2"><path stroke-dasharray="36" stroke-dashoffset="36" stroke-width="5" d="M12 7c2.76 0 5 2.24 5 5c0 2.76 -2.24 5 -5 5c-2.76 0 -5 -2.24 -5 -5c0 -2.76 2.24 -5 5 -5Z"><animate fill="freeze" attributeName="stroke-dashoffset" dur="0.5s" values="36;0"/><set fill="freeze" attributeName="opacity" begin="0.5s" to="0"/></path><g fill="#fff" stroke="none" opacity="0"><use href="#SVGKKfyAcRD"/><use href="#SVGKKfyAcRD" transform="rotate(60 12 12)"/><use href="#SVGKKfyAcRD" transform="rotate(120 12 12)"/><use href="#SVGKKfyAcRD" transform="rotate(180 12 12)"/><use href="#SVGKKfyAcRD" transform="rotate(240 12 12)"/><use href="#SVGKKfyAcRD" transform="rotate(300 12 12)"/><set fill="freeze" attributeName="opacity" begin="0.5s" to="1"/><animateTransform attributeName="transform" dur="30s" repeatCount="indefinite" type="rotate" values="0 12 12;360 12 12"/></g></g><circle cx="12" cy="12" r="3.5"/></mask><rect width="24" height="24" fill="currentColor" mask="url(#SVGzJbIMdSz)"/></svg>',
                'eye-outline':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zm0 12c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8a3 3 0 100 6 3 3 0 000-6z"/></svg>',
                'notification-settings-outline':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.63-5.64-4.5-6.32V4c0-1.1-.9-2-2-2s-2 .9-2 2v.68C6.63 5.36 5 7.93 5 11v5l-2 2v1h18v-1l-2-2zm-3.5-6.5l1.5-1.5-1.5-1.5v2zm-3 0l1.5-1.5-1.5-1.5v2z"/></svg>',
                access: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M7 2a5 5 0 0 0-2.5 9.331v6.512c0 .818 0 1.226.152 1.594c.152.367.442.656 1.02 1.235L7 22l2.108-2.108c.097-.097.146-.146.186-.199a1 1 0 0 0 .197-.475c.009-.066.009-.135.009-.272c0-.111 0-.167-.006-.221a1 1 0 0 0-.134-.401a2 2 0 0 0-.128-.18L8 16.5l.7-.933c.396-.529.595-.793.697-1.101c.103-.308.103-.638.103-1.3v-1.835A5 5 0 0 0 7 2m0 5h.009M13 14h6c.932 0 1.398 0 1.765.152a2 2 0 0 1 1.083 1.083C22 15.602 22 16.068 22 17s0 1.398-.152 1.765a2 2 0 0 1-1.083 1.083C20.398 20 19.932 20 19 20h-6m2-15h4c.932 0 1.398 0 1.765.152a2 2 0 0 1 1.083 1.083C22 6.602 22 7.068 22 8s0 1.398-.152 1.765a2 2 0 0 1-1.083 1.083C20.398 11 19.932 11 19 11h-4"/></svg>',
                'delete-20-regular':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 20 20"><path fill="currentColor" d="M8.5 4h3a1.5 1.5 0 0 0-3 0m-1 0a2.5 2.5 0 0 1 5 0h5a.5.5 0 0 1 0 1h-1.054l-1.194 10.344A3 3 0 0 1 12.272 18H7.728a3 3 0 0 1-2.98-2.656L3.554 5H2.5a.5.5 0 0 1 0-1zM5.741 15.23A2 2 0 0 0 7.728 17h4.544a2 2 0 0 0 1.987-1.77L15.439 5H4.561zM8.5 7.5A.5.5 0 0 1 9 8v6a.5.5 0 0 1-1 0V8a.5.5 0 0 1 .5-.5M12 8a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0z"/></svg>',
                'camera-cog':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path d="M12 20H5a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h1a2 2 0 0 0 2-2a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1a2 2 0 0 0 2 2h1a2 2 0 0 1 2 2v3"/><path d="M14.973 13.406A3 3 0 1 0 12 16m5.001 3a2 2 0 1 0 4 0a2 2 0 1 0-4 0m2-3.5V17m0 4v1.5m3.031-5.25l-1.299.75m-3.463 2l-1.3.75m0-3.5l1.3.75m3.463 2l1.3.75"/></g></svg>',
                dashboard:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 32 32"><path fill="currentColor" d="M4 4h10v10H4zm12 0h10v10H16zm-12 12h10v10H4zm12 12h10v-10H16z"/></svg>',
                'shipping-truck':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M20 8h-3V6.5A2.5 2.5 0 0 0 14.5 4h-9A2.5 2.5 0 0 0 3 6.5v10A2.5 2.5 0 0 0 5.5 19c.28 1.72 1.75 3 3.5 3s3.22-1.28 3.5-3h1c.28 1.72 1.75 3 3.5 3s3.22-1.28 3.5-3H21v-6l-1-3zm-2.5 11c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm-9 0c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zM4 6.5c0-.83.67-1.5 1.5-1.5h9c.83 0 1.5.67 1.5 1.5V8H4V6.5zm0 4.5h12v4H4v-4z"/></svg>',
                'document-report':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M8 3v.01M12 3v.01M16 3v.01M4 7h16M6 11h2v6H6zm4 0h2v4h-2zm4 0h2v8h-2zM5 19h14a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v10a2 2 0 0 0 2 2z"/></svg>',
                'clipboard-list':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M16 6l2.29 2.29l-4.88 4.88l-4-4L11.7 6zm-2 7.41L17.71 9.7l-1.42-1.42L12 12.59L7.71 8.29L6.29 9.71l4 4zM4 3h6c0-1.11.89-2 2-2s2 .89 2 2h6v2H4V3zm0 4h16v14H4V7z"/></svg>',
                'sunny-filled-loop-to-moon-alt-filled-loop-transition':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2a10 10 0 00-7.07 17.07A10 10 0 0012 2zm0 18a8 8 0 110-16 8 8 0 010 16z"><animateTransform attributeName="transform" type="rotate" from="0 12 12" to="360 12 12" dur="1s" repeatCount="indefinite"/></path></svg>',

                account:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 4a4 4 0 1 0 0 8a4 4 0 0 0 0-8M6 8a6 6 0 1 1 12 0A6 6 0 0 1 6 8m2 10v1a3 3 0 0 0 3 3h2a3 3 0 0 0 3-3v-1a4 4 0 0 0-8 0"/></svg>',
                'plus-thick':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/></svg>',
                'plus-box':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M19 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2zm-8 13H9v-3H6v-2h3V8h2v3h3v2h-3v3z"/></svg>',
                'minus-box':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M19 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V5a2 2 0 0 0-2-2zm-2 10H7v-2h10v2z"/></svg>',
                location:
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5a2.5 2.5 0 010-5 2.5 2.5 0 010 5z"/></svg>',
                'save-outline':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-width="2" d="M17 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7l-2-4zm-5 16c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zm3-10H9V3h6v6z"/></svg>',
                'edit-twotone':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" fill-opacity=".5" d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25z"/><path fill="currentColor" d="M20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34a.996.996 0 0 0-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/></svg>',
                'arrow-small-left':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M15.41 16.59L10.83 12l4.58-4.59L14 6l-6 6l6 6l1.41-1.41z"/></svg>',
                cancel: '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z"/></svg>',
                'round-map':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M20.5 3l-.16.03L15 5.1 9 3 3.36 4.9c-.21.07-.36.25-.36.48V20.5c0 .28.22.5.5.5l.16-.03L9 18.9l6 2.1 5.64-1.9c.21-.07.36-.25.36-.48V3.5c0-.28-.22-.5-.5-.5zM15 19l-6-2.11V5l6 2.11V19z"/></svg>',
                'play-twotone':
                    '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" fill-opacity=".3" d="M8 5v14l11-7z"/><path fill="currentColor" d="M8 5v14l11-7L8 5z"/></svg>',
            },
        }
    },
    methods: {
        getIconSvg() {
            const iconName = this.icon.includes(':')
                ? this.icon.split(':').pop()
                : this.icon
            let svgContent = this.icons[iconName]

            if (!svgContent) {
                // Trường hợp không tìm thấy icon, trả về icon mặc định
                return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zM11 7h2v6h-2V7zm0 8h2v2h-2v-2z"/></svg>'
            }

            // Đảm bảo icon có màu sắc bằng cách thêm fill hoặc stroke
            if (
                svgContent.includes('fill="none"') &&
                !svgContent.includes('stroke=')
            ) {
                svgContent = svgContent.replace(
                    /<path/g,
                    '<path stroke="currentColor"'
                )
            } else if (
                !svgContent.includes('fill=') &&
                !svgContent.includes('stroke=')
            ) {
                svgContent = svgContent.replace(
                    /<path/g,
                    '<path fill="currentColor"'
                )
            }

            return svgContent
        },
    },
}
</script>

<style scoped>
.icon-wrapper {
    display: inline-flex; /* Sử dụng flexbox để căn giữa dễ dàng */
    justify-content: center; /* Căn ngang */
    align-items: center; /* Căn dọc */
    width: 24px;
    height: 24px;
    color: #667085; /* Màu mặc định cho icon */
}

/* Đảm bảo SVG bên trong span luôn lấp đầy khung 24x24px */
.icon-wrapper svg {
    width: 100%;
    height: 100%;
    box-sizing: border-box; /* Đảm bảo padding không làm vỡ kích thước */
}

/* Thêm style cho các button chứa icon trong cột "Thao tác" */
.action-btn {
    display: inline-flex;
    justify-content: center;
    align-items: center;
    width: 32px; /* Tăng kích thước button để icon có không gian */
    height: 32px;
    border: 1px solid #ccc;
    border-radius: 4px;
    cursor: pointer;
    margin-right: 8px;
}

/* Hover effect cho button */
.action-btn:hover {
    background-color: #f0f0f0;
}

.menu-item {
    display: flex;
    align-items: center;
    color: #667085; /* Màu text mặc định */
}

/* Style khi di chuột qua */
.menu-item:hover {
    background-color: #f0f0f0; /* Màu nền khi hover */
}

/* Style cho item đang được chọn */
.menu-item.active {
    background-color: #007bff; /* Màu nền xanh */
    color: white; /* Màu text trắng */
}
</style>
