<template>
    <div>
        <b-sidebar
            id="sidebar-task-handler"
            sidebar-class="sidebar-lg"
            :visible="isTaskHandlerSidebarActive"
            bg-variant="white"
            shadow
            backdrop
            no-header
            right
        />
    </div>
</template>
<script>
/* eslint-disable */
export default {
    model: {
        prop: 'isTaskHandlerSidebarActive',
    },
}
</script>
