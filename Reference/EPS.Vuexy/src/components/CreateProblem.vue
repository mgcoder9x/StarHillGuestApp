<template>
    <validation-observer ref="rules">
        <b-card-body>
            <div class="p-6 shadow-md rounded-lg max-w-3xl mx-auto">
                <b-row>
                    <b-col md="7">
                        <b-row>
                            <b-col>
                                <b-form-group :label="this.$t('Nhà thầu')" label-cols-md="3" label-class="required">
                                    <validation-provider #default="{ errors }" rules="required"
                                        :name="$t('Issue.Label.Project')">
                                        <v-select v-model="createData.contractorId" label="text"
                                            :reduce="(area) => area.id" :options="listContractor"
                                            @input="changeContractorId($event)">
                                        </v-select>
                                        <small class="text-danger">{{ errors[0] }}</small>
                                    </validation-provider>
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <b-col>
                                <b-form-group :label="this.$t('Nhân sự vi phạm')" label-cols-md="3">
                                    <v-select v-model="createData.personId" label="text" :reduce="(emp) => emp.value"
                                        :options="listEmployee" @input="changepersonId($event)">
                                    </v-select>
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <b-col md="6">
                                <b-form-group :label="this.$t('Giới tính')" label-cols-md="3">
                                    <b-form-input v-model="createData.genderStr" disabled />
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group :label="this.$t('Năm sinh')" label-cols-md="3">
                                    <b-form-input v-model="createData.birthdayStr" disabled />
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <b-col md="6">
                                <b-form-group :label="this.$t('Loại vi phạm')" label-cols-md="3">
                                    <b-form-input v-model="createData.eventTypeName" disabled />
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group :label="this.$t('Lỗi vi phạm')" label-cols-md="3">
                                    <b-form-input v-model="createData.warningName" disabled />
                                </b-form-group>
                            </b-col>
                        </b-row>
                        <b-row>
                            <b-col md="6">
                                <b-form-group :label="this.$t('Thời gian')" label-cols-md="3">
                                    <b-form-input v-model="createData.accessTimeStr" disabled />
                                </b-form-group>
                            </b-col>
                            <b-col md="6">
                                <b-form-group :label="this.$t('Khu vực vi phạm')" label-cols-md="3">
                                    <b-form-input v-model="createData.areaName" disabled />
                                </b-form-group>
                            </b-col>
                        </b-row>
                    </b-col>
                    <b-col md="5">
                        <b-row>
                            <img 
                                :src="`${baseUrl}${createData.image}`"
                                class="img-fluid cursor-pointer"
                                style="object-fit: cover; border-radius: 0.3em"
                                loading="lazy" 
                            />
                        </b-row>
                    </b-col>
                </b-row>

                <b-col class="text-center">
                    <b-button @click="createIssue" variant="success">
                        {{ $t('Button.Create') }}
                    </b-button>
                </b-col>
            </div>
        </b-card-body>
    </validation-observer>

</template>

<script>
import ToastificationContent from '@core/components/toastification/ToastificationContent.vue'

export default {
    components: {},
    props: {
        newData: {
            type: Object,
            required: true
        }
    },
    data() {
        return {
            createData: {
                contractorId: null,
                personId: null,
                genderStr: null,
                birthdayStr: null,
                eventTypeId: null,
                warningLevelId: null,
                accessTime: null,
                areaId: null,
                image: null
            },
            listContractor: [],
            listEmployee: [],
            listWarningLevelId: [],
            listGender: [
                { value: 0, text: 'Nữ' },
                { value: 1, text: 'Nam' },
            ],
        }
    },
    computed:{
        baseUrl() {
            return process.env.VUE_APP_BASE_URL
            //return "https://novaland.atin.vn/Service/"
        },
    },
    created() {
        this.loadContractor()
        this.newData.genderStr = null
        this.newData.birthdayStr = null
        this.createData = JSON.parse(JSON.stringify(this.newData))
        this.createData.genderStr = null
        this.createData.birthdayStr = null
        // if (this.createData.eventId) {
        //     console.log(this.createData)
        //     this.getData()
        // }
        console.log(this.createData)
    },
    methods: {
        changepersonId(event) {
            console.log(event)
            this.createData.genderStr = null
            this.createData.birthdayStr = null
            if (event) {
                var employee = this.listEmployee.find(x => x.id == event)
                console.log('employy', employee)

                this.createData.birthdayStr = employee.birthdayStr
                if (employee.gender == 1) {
                    this.createData.genderStr = "Nam"
                }
                else if (employee.gender == 0) {
                    this.createData.genderStr = "Nữ"
                }
                else {
                    this.createData.genderStr = ""
                }
                console.log('createdata', this.createData)
            }
        },
        changeContractorId(event) {
            this.createData.genderStr = null
            this.createData.birthdayStr = null
            this.createData.personId = null
            if (event) {
                this.$services.get(`/lookup/departments/${event}/employees`).then((response) => {
                    this.listEmployee = response.data.data
                })
            }
            else {
                this.listEmployee = []
            }
        },
        async getData() {
            const response = await this.$services.get(
                `/event/eventFilesById/${this.createData.eventId}`
            )
            const newData = response.data.map(x => ({
                ...x,
                id: null,
                isAuto: true
            }))
        },
        getUrl(path) {
            return `${process.env.VUE_APP_BASE_URL}/IMSFile/${path}`
            //return `http://192.168.1.85:42001/Service/IMSFile/${path}`
        },
        getUrlIsAuto(path) {
            return `${process.env.VUE_APP_BASE_URL}${path}`
            //return `http://192.168.1.85:42001/Service/${path}`
        },
        loadContractor() {
            this.$services.get('/lookup/departments?type=2').then((response) => {
                this.listContractor = response.data.data
            })
        },
        createIssue(e) {
            e.preventDefault()
            this.$refs.rules.validate().then(async (success) => {
                if (success) {
                    this.$services.post('/problem', this.createData)
                        .then((response) => {
                            this.$emit("success")
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t(`Success.Create`),
                                    icon: 'CheckIcon',
                                    variant: 'success',
                                },
                            })
                        })
                        .catch((error) => {
                            this.$toast({
                                component: ToastificationContent,
                                position: 'top-right',
                                props: {
                                    title: this.$t('Error.Error'),
                                    icon: 'AlertTriangleIcon',
                                    variant: 'danger',
                                    text: `${this.$t(error.response.data.message)}`,
                                },
                            })
                        })
                }
            })

        }
    }
}
</script>
<style></style>
