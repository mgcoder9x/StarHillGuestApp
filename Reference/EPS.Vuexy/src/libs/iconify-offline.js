/**
 * Iconify Offline Configuration
 * Load icon sets locally without API calls
 */
import { addCollection } from '@iconify/vue2'

// Import all icon collections
import mdiIcons from '@iconify-json/mdi/icons.json'
import tablerIcons from '@iconify-json/tabler/icons.json'
import materialSymbolsIcons from '@iconify-json/material-symbols/icons.json'
import lucideIcons from '@iconify-json/lucide/icons.json'
import carbonIcons from '@iconify-json/carbon/icons.json'
import antDesignIcons from '@iconify-json/ant-design/icons.json'
import biIcons from '@iconify-json/bi/icons.json'
import clarityIcons from '@iconify-json/clarity/icons.json'
import fluentIcons from '@iconify-json/fluent/icons.json'
import solarIcons from '@iconify-json/solar/icons.json'
import mingcuteIcons from '@iconify-json/mingcute/icons.json'
import hugeiconsIcons from '@iconify-json/hugeicons/icons.json'
import icIcons from '@iconify-json/ic/icons.json'
import iconParkOutlineIcons from '@iconify-json/icon-park-outline/icons.json'
import lineMdIcons from '@iconify-json/line-md/icons.json'
import ouiIcons from '@iconify-json/oui/icons.json'
import quillIcons from '@iconify-json/quill/icons.json'
import tdesignIcons from '@iconify-json/tdesign/icons.json'
import uilIcons from '@iconify-json/uil/icons.json'
import eiIcons from '@iconify-json/ei/icons.json'
import elIcons from '@iconify-json/el/icons.json'
import fileIconsIcons from '@iconify-json/file-icons/icons.json'
import gridicons from '@iconify-json/gridicons/icons.json'
import iconoirIcons from '@iconify-json/iconoir/icons.json'
import nimbusIcons from '@iconify-json/nimbus/icons.json'

// Load all icon sets
const loadIconSets = () => {
  const iconSets = [
    { name: 'MDI', data: mdiIcons },
    { name: 'Tabler', data: tablerIcons },
    { name: 'Material Symbols', data: materialSymbolsIcons },
    { name: 'Lucide', data: lucideIcons },
    { name: 'Carbon', data: carbonIcons },
    { name: 'Ant Design', data: antDesignIcons },
    { name: 'Bootstrap Icons', data: biIcons },
    { name: 'Clarity', data: clarityIcons },
    { name: 'Fluent', data: fluentIcons },
    { name: 'Solar', data: solarIcons },
    { name: 'Mingcute', data: mingcuteIcons },
    { name: 'Hugeicons', data: hugeiconsIcons },
    { name: 'IC', data: icIcons },
    { name: 'Icon Park Outline', data: iconParkOutlineIcons },
    { name: 'Line MD', data: lineMdIcons },
    { name: 'OUI', data: ouiIcons },
    { name: 'Quill', data: quillIcons },
    { name: 'TDesign', data: tdesignIcons },
    { name: 'UIL', data: uilIcons },
    { name: 'EI', data: eiIcons },
    { name: 'EL', data: elIcons },
    { name: 'File Icons', data: fileIconsIcons },
    { name: 'Gridicons', data: gridicons },
    { name: 'Iconoir', data: iconoirIcons },
    { name: 'Nimbus', data: nimbusIcons },
  ]

  iconSets.forEach(({ name, data }) => {
    try {
      addCollection(data)
      console.log(`✅ Loaded ${name} icons offline (${Object.keys(data.icons || {}).length} icons)`)
    } catch (e) {
      console.warn(`⚠️ Failed to load ${name} icons:`, e.message)
    }
  })

  console.log('🎉 All icon sets loaded for offline use!')
}

export default loadIconSets