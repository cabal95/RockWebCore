<div id="$$id$$">
    <h3>{{ Title }}</h3>
    <div v-html="Description"></div>

    <item v-for="item in Items" v-bind:key="item.Id" v-bind:item="item" v-bind:hide-selected="true" v-on:set-state="onSetState"></item>
</div>