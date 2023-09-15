;; invoke: add (f32:1.23) (f32:2.34)
;; expect: (f32:3.57)

(module
    (func (export "add") (param $x f32) (param $y f32) (result f32)
        local.get $x
        local.get $y
        f32.add
    )
)